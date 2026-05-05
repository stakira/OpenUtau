-- English Arpasing Phonemizer (Lua port)
-- Arpasing is a diphone system using CMUdict-style phoneme symbols.
-- See: http://www.speech.cs.cmu.edu/cgi-bin/cmudict

local CONSONANT_LENGTH = 60  -- ticks for each leading consonant

local VOWEL_FALLBACK = {
    aa = {"ah", "ae"},
    ae = {"ah", "aa"},
    ah = {"aa", "ae"},
    ao = {"ow"},
    ow = {"ao"},
    eh = {"ae"},
    ih = {"iy"},
    iy = {"ih"},
    uh = {"uw"},
    uw = {"uh"},
    aw = {"ao"},
}

-- Phoneme type tables (populated from arpasing.yaml)
local vowels    = {}  -- set of vowel symbols
local glides    = {}  -- set of glide/semivowel symbols
local dict      = {}  -- grapheme → {phoneme list}
local valid_sym = {}  -- set of all valid symbols

local current_singer = nil

-- ─── Dictionary loading ────────────────────────────────────────────────────

local function load_dictionary(path)
    local data = openutau.yaml.load(path)
    -- Load symbol types
    local syms = data["symbols"]
    if syms then
        for i = 1, #syms do
            local s = syms[i]
            local sym  = s["symbol"]
            local stype = s["type"]
            if sym and stype then
                valid_sym[sym] = true
                if stype == "vowel" then
                    vowels[sym] = true
                elseif stype == "semivowel" or stype == "liquid" then
                    glides[sym] = true
                end
            end
        end
    end
    -- Load grapheme→phoneme entries
    local entries = data["entries"]
    if entries then
        for i = 1, #entries do
            local e = entries[i]
            local grapheme = e["grapheme"]
            local phonemes = e["phonemes"]
            if grapheme and phonemes then
                local list = {}
                for j = 1, #phonemes do
                    list[j] = phonemes[j]
                end
                dict[grapheme:lower()] = list
            end
        end
    end
end

-- ─── Phonemizer lifecycle ──────────────────────────────────────────────────

function get_info()
    return {
        name     = "English Arpasing Phonemizer (Lua)",
        tag      = "EN ARPA LUA",
        author   = "OpenUtau",
        language = "EN",
    }
end

function set_singer(singer)
    current_singer = singer

    -- Reset tables so we can reload
    vowels    = {}
    glides    = {}
    dict      = {}
    valid_sym = {}

    -- Load bundled dictionary
    load_dictionary("package:arpasing/arpasing.yaml")

    -- Override/extend with singer-specific dictionary if present
    if singer and singer.location and singer.location ~= "" then
        local singer_dict = "singer:arpasing.yaml"
        local f = io.open(singer_dict, "r")
        if f then
            f:close()
            load_dictionary(singer_dict)
        end
    end
end

-- ─── Symbol helpers ────────────────────────────────────────────────────────

local function get_symbols(note)
    -- Phonetic hint overrides lookup
    if note.phonetic_hint and note.phonetic_hint ~= "" then
        local parts = {}
        for sym in note.phonetic_hint:gmatch("%S+") do
            if valid_sym[sym] then
                parts[#parts + 1] = sym
            end
        end
        return parts
    end
    -- User YAML dictionary lookup
    local lyric = note.lyric:lower()
    local syms = dict[lyric]
    if syms then
        local copy = {}
        for i = 1, #syms do copy[i] = syms[i] end
        return copy
    end
    -- Neural G2P fallback via C# bridge
    if openutau.g2p then
        local neural = openutau.g2p.query(lyric)
        if neural then
            local filtered = {}
            for i = 1, #neural do
                if valid_sym[neural[i]] then
                    filtered[#filtered + 1] = neural[i]
                end
            end
            if #filtered > 0 then return filtered end
        end
    end
    -- No entry found; treat lyric as a single alias
    return nil
end

local function is_vowel(sym)  return vowels[sym] == true end
local function is_glide(sym)  return glides[sym] == true end

-- ─── Alias selection ───────────────────────────────────────────────────────

local function get_phoneme_or_fallback(prev_sym, sym, tone, color, alt)
    local singer = current_singer
    if not singer then return prev_sym .. " " .. sym .. (alt or "") end

    -- Try diphone with alternate suffix
    if alt and alt ~= "" then
        local mapped = singer:try_map_phoneme(prev_sym .. " " .. sym .. alt, tone, color)
        if mapped then return mapped end
    end
    -- Try standard diphone
    local mapped = singer:try_map_phoneme(prev_sym .. " " .. sym, tone, color)
    if mapped then return mapped end
    -- Try vowel fallback chain
    local fallbacks = VOWEL_FALLBACK[sym]
    if fallbacks then
        for _, fb in ipairs(fallbacks) do
            local fb_mapped = singer:try_map_phoneme(prev_sym .. " " .. fb, tone, color)
            if fb_mapped then return fb_mapped end
        end
    end
    -- Try leading consonant
    local lc_mapped = singer:try_map_phoneme("- " .. sym, tone, color)
    if lc_mapped then return lc_mapped end
    -- Default: return raw diphone string
    return prev_sym .. " " .. sym .. (alt or "")
end

-- ─── Timing distribution ──────────────────────────────────────────────────

local function distribute_duration(is_vowel_arr, phonemes, start_idx, end_idx, start_tick, end_tick)
    if start_idx >= end_idx then return end
    local consonants = 0
    local vowel_count = 0
    local duration = end_tick - start_tick
    for i = start_idx, end_idx - 1 do
        if is_vowel_arr[i] then vowel_count = vowel_count + 1
        else consonants = consonants + 1 end
    end
    local consonant_dur
    if vowel_count > 0 then
        consonant_dur = consonants > 0
            and math.min(CONSONANT_LENGTH, math.floor(duration / 2 / consonants))
            or 0
    else
        consonant_dur = consonants > 0 and math.floor(duration / consonants) or 0
    end
    local vowel_dur = vowel_count > 0
        and math.floor((duration - consonant_dur * consonants) / vowel_count)
        or 0
    local pos = start_tick
    for i = start_idx, end_idx - 1 do
        phonemes[i].position = pos
        if is_vowel_arr[i] then pos = pos + vowel_dur
        else pos = pos + consonant_dur end
    end
end

-- ─── Main process ──────────────────────────────────────────────────────────

function process(notes, ctx)
    local note = notes[1]

    -- Force alias with ? prefix
    if note.lyric:sub(1, 1) == "?" then
        return {{ phoneme = note.lyric:sub(2), position = 0 }}
    end

    -- Previous note's symbols for connecting diphones
    local prev_symbols = nil
    if ctx.prev and ctx.prev_adjacent then
        prev_symbols = get_symbols(ctx.prev)
    end

    -- Handle "-" tail note
    if note.lyric == "-" and prev_symbols and #prev_symbols > 0 then
        local alias = prev_symbols[#prev_symbols] .. " -"
        local mapped = current_singer and current_singer:try_map_phoneme(alias, note.tone, "")
        return {{ phoneme = mapped or alias, position = 0 }}
    end

    -- Get phoneme symbols for this note
    local symbols = get_symbols(note)
    local add_tail = not ctx.next_adjacent
    if add_tail and symbols then
        symbols[#symbols + 1] = "-"
    end
    if not symbols or #symbols == 0 then
        return {{ phoneme = note.lyric, position = 0 }}
    end

    local n = #symbols
    local is_vowel_arr = {}
    local is_glide_arr = {}
    for i = 1, n do
        is_vowel_arr[i] = is_vowel(symbols[i])
        is_glide_arr[i] = is_glide(symbols[i])
    end

    -- Phoneme slot array (position filled in during distribution)
    local phonemes = {}
    for i = 1, n do
        phonemes[i] = { phoneme = "", position = 0 }
    end

    -- Build alignment points: each non-extension note gets one vowel aligned to it
    local alignments = {}
    local non_ext_notes = {}
    for i = 1, #notes do
        local l = notes[i].lyric
        if not (l:sub(1, 2) == "+~" or l:sub(1, 2) == "+*") then
            non_ext_notes[#non_ext_notes + 1] = notes[i]
        end
    end

    local align_count = 0
    for i = 1, n do
        if is_vowel_arr[i] and align_count < #non_ext_notes then
            align_count = align_count + 1
            local tick = non_ext_notes[align_count].position - note.position
            -- C-G-V: align the glide instead of the vowel
            if i >= 3 and is_glide_arr[i - 1] and not is_vowel_arr[i - 2] then
                alignments[#alignments + 1] = { idx = i - 1, tick = tick, manual = false }
            else
                alignments[#alignments + 1] = { idx = i, tick = tick, manual = false }
            end
        end
    end

    -- Manual alignments from "+N" extension notes
    local total_tick = notes[1].duration
    for i = 2, #notes do
        local l = notes[i].lyric
        if l:sub(1, 1) == "+" then
            local num = tonumber(l:sub(2))
            if num then
                alignments[#alignments + 1] = { idx = num, tick = total_tick, manual = true }
            end
        end
        total_tick = total_tick + notes[i].duration
    end
    -- Sentinel
    alignments[#alignments + 1] = { idx = n + 1, tick = total_tick, manual = true }

    -- Sort by phoneme index
    table.sort(alignments, function(a, b) return a.idx < b.idx end)

    -- Remove conflicting manual alignments
    local i = 1
    while i <= #alignments do
        if alignments[i].manual then
            while i > 1 and (alignments[i-1].tick >= alignments[i].tick
                          or alignments[i-1].idx == alignments[i].idx) do
                table.remove(alignments, i - 1)
                i = i - 1
            end
            while i < #alignments and (alignments[i+1].tick <= alignments[i].tick
                                    or alignments[i+1].idx == alignments[i].idx) do
                table.remove(alignments, i + 1)
            end
        end
        i = i + 1
    end

    -- Find first vowel for initial offset (Arpasing aligns first vowel at tick 0)
    local first_vowel = 0
    for i = 1, n do
        if is_vowel_arr[i] then
            first_vowel = i - 1
            break
        end
    end

    local start_idx = 1
    local start_tick = -CONSONANT_LENGTH * first_vowel
    for _, al in ipairs(alignments) do
        distribute_duration(is_vowel_arr, phonemes, start_idx, al.idx, start_tick, al.tick)
        start_idx = al.idx
        start_tick = al.tick
    end

    -- Select aliases (diphone lookup with fallback)
    local prev_sym = (prev_symbols and #prev_symbols > 0) and prev_symbols[#prev_symbols] or "-"
    local note_idx = 1
    for i = 1, n do
        while note_idx < #notes and notes[note_idx].position - note.position < phonemes[i].position do
            note_idx = note_idx + 1
        end
        local tone = notes[note_idx].tone
        local color = ""
        local alt   = ""
        phonemes[i].phoneme = get_phoneme_or_fallback(prev_sym, symbols[i], tone, color, alt)
        prev_sym = symbols[i]
    end

    return phonemes
end
