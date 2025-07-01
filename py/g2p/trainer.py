import math
import random
import torch
import tqdm
import editdistance
from torch import nn
from torch.optim import Adam
from torch.optim.lr_scheduler import ExponentialLR
from torch.utils.data import Dataset, DataLoader
from torch.utils.data.dataset import random_split
from torch.nn.utils.rnn import pad_sequence
from torchaudio.transforms import RNNTLoss


UNK_IDX, PAD_IDX, BOS_IDX, EOS_IDX = 0, 1, 2, 3
special_symbols = ['<unk>', '<pad>', '<bos>', '<eos>']


class G2pTrainer():
    def __init__(self,
                 device: torch.device,
                 loss_device: torch.device,
                 model: nn.Module,
                 dataset: Dataset,
                 batch_size: int = 256,
                 epochs: int = 20,
                 lr: float = 0.005,
                 min_lr: float = 5e-5,
                 gamma: float = 0.8,
                 grad_clip: float = 2,
                 seed=None) -> None:
        self.device = device
        self.loss_device = loss_device
        self.model = model
        self.dataset = dataset
        self.epochs = epochs
        self.min_lr = min_lr
        self.grad_clip = grad_clip
        self.model.to(device)

        self.loss_fn = RNNTLoss(blank=BOS_IDX, clamp=self.grad_clip)
        self.optimizer = Adam(model.parameters(), lr=lr,
                              betas=(0.9, 0.98), eps=1e-9)
        self.scheduler = ExponentialLR(
            self.optimizer, gamma=gamma, verbose=True)

        valid_set_size = len(dataset) // 10
        train_set_size = len(dataset) - valid_set_size
        g = torch.Generator()
        if seed is not None:
            g.manual_seed(seed)
        valid_set, train_set = random_split(
            dataset, [valid_set_size, train_set_size], g)

        def collate_fn(batch):
            src_batch, tgt_batch = [], []
            for src, tgt in batch:
                src_batch.append(src)
                tgt_batch.append(tgt)
            src_lengths = torch.tensor([len(s)
                                       for s in src_batch], dtype=torch.int32)
            src_batch = pad_sequence(src_batch, padding_value=0)
            tgt_lengths = torch.tensor([len(s)
                                       for s in tgt_batch], dtype=torch.int32)
            tgt_batch = pad_sequence(tgt_batch, padding_value=0)
            return src_batch.transpose(0, 1).contiguous(), tgt_batch.transpose(
                0, 1).contiguous(), src_lengths, tgt_lengths

        self.train_dl = DataLoader(
            train_set, batch_size, True, collate_fn=collate_fn)
        self.valid_dl = DataLoader(
            valid_set, batch_size, collate_fn=collate_fn)

    def _accuracy(self, logits, tgt_true):
        tgt_pred = torch.argmax(logits, dim=-1)
        correct = torch.sum(torch.logical_and(
            tgt_pred == tgt_true, torch.logical_or(
                tgt_pred != 0, tgt_true != 0)).int()).item()
        total = torch.sum((tgt_true != 0).int()).item()
        return correct, total

    def _train_epoch(self):
        self.model.train()
        losses = 0
        count = 0
        pbar = tqdm.tqdm(self.train_dl)
        for src, tgt, src_lengths, tgt_lengths in pbar:
            tgt_in = torch.concat(
                [torch.full([tgt.shape[0], 1], BOS_IDX), tgt], dim=1)
            logits = self.model(src.to(self.device), tgt_in.to(self.device))

            self.optimizer.zero_grad()
            loss = self.loss_fn(
                logits.to(self.loss_device),
                tgt.to(self.loss_device),
                src_lengths.to(self.loss_device),
                tgt_lengths.to(self.loss_device))
            loss.backward()

            torch.nn.utils.clip_grad.clip_grad_norm_(
                self.model.parameters(), self.grad_clip)
            self.optimizer.step()
            losses += loss.item()
            count += 1

            pbar.set_description('loss: {:.4f}'.format(losses / count))
        return losses / count

    def _eval(self):
        self.model.eval()
        losses = 0
        count = 0
        for src, tgt, src_lengths, tgt_lengths in self.valid_dl:
            tgt_in = torch.concat(
                [torch.full([tgt.shape[0], 1], BOS_IDX), tgt], dim=1)
            logits = self.model(src.to(self.device), tgt_in.to(self.device))

            loss = self.loss_fn(
                logits.to(self.loss_device),
                tgt.to(self.loss_device),
                src_lengths.to(self.loss_device),
                tgt_lengths.to(self.loss_device))

            losses += loss.item()
            count += 1

        return losses / count

    def _save_state_dic(self, name):
        torch.save(self.model.state_dict(), 'g2p-{}.ptsd'.format(name))

    def _load_state_dic(self, name):
        self.model.load_state_dict(torch.load('g2p-{}.ptsd'.format(name)))

    def _preview(self, entry):
        word, pron = entry
        print('{}: [{}] [{}]'.format(word, '-'.join(pron),
              '-'.join(self.model.predict_str(word))))

    def train(self):
        preview_entries = []
        for i in range(5):
            idx = random.randrange(len(self.dataset.entries))
            preview_entries.append(self.dataset.entries[idx])

        best_eval_loss = 10000
        for i in range(self.epochs):
            loss = self._train_epoch()
            eval_loss = self._eval()
            lr = self.scheduler.get_last_lr()[0]
            print('epoch: {} - lr: {:.2e} - loss: {:.4f} - eval_loss: {:.4f}'
                  .format(i, lr, loss, eval_loss))
            if math.isnan(loss) or math.isnan(eval_loss):
                break

            if lr > self.min_lr:
                self.scheduler.step()

            if best_eval_loss > eval_loss:
                best_eval_loss = eval_loss
                print('saving new best at epoch {}'.format(i))
                self._save_state_dic('best')
            if (i + 1) % 20 == 0:
                self._save_state_dic('{:03d}'.format(i + 1))

            for entry in preview_entries:
                self._preview(entry)

    # Calculates the WER and PER of the model on the entire dataset.
    # Very very slow.
    def test(self, test_log=None):
        word_count = 0
        word_error = 0
        phoneme_count = 0
        phoneme_error = 0
        pbar = tqdm.tqdm(self.dataset.entries)
        if test_log is not None:
            f = open(test_log, 'w', encoding='utf8')
        for word, pron in pbar:
            predicted = self.model.predict_str(word)
            dis = editdistance.distance(pron, predicted)
            word_count += 1
            phoneme_count += len(pron)
            if dis > 0:
                word_error += 1
                phoneme_error += dis
                if test_log is not None:
                    f.write('{}\n\t{}\n\t{}\n'.format(
                        word, ' '.join(pron), ' '.join(predicted)))
            pbar.set_description("wer = {:.4f} per = {:.4f}".format(
                word_error / word_count, phoneme_error / phoneme_count))
        if test_log is not None:
            f.close()
