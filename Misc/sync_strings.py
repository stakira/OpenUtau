import os
import re
import xml.etree.ElementTree as ET
import xml.dom.minidom as MD
import json

def register_all_namespaces(filename):
    namespaces = dict(
        [node for _, node in ET.iterparse(filename, events=['start-ns'])])
    for ns in namespaces:
        ET.register_namespace(ns, namespaces[ns])


def file_to_dict(filename):
    tag_class = re.compile(r"\{.*\}")
    with open(filename, "r", encoding='utf8') as f:
        src_etree = ET.parse(f)
    return {
        child.get('{http://schemas.microsoft.com/winfx/2006/xaml}Key'):
        (tag_class.sub('', child.tag), child.text)
        for child in src_etree.getroot()[:]
        if (child.text != "" and child.text is not None)
    }


def dict_to_file(filename, dict, en_dict):
    with open(filename, "w", encoding='utf8') as f:
        f.write('<ResourceDictionary xmlns="https://github.com/avaloniaui"\n')
        f.write(
            '                    xmlns:system="clr-namespace:System;assembly=mscorlib"\n')
        f.write(
            '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n')
        last_section = None
        for key in sorted(dict.keys()):
            try:
                section = key[:key.index('.')]
            except ValueError:
                section = key
            if last_section != section:
                f.write('\n')
            last_section = section
            tag = dict[key][0]
            if tag == 'String':
                tag = 'system:String'
            text = dict[key][1]
            line = '<%s x:Key="%s">%s</%s>' % (
                tag, key, text, tag)
            if en_dict and text == en_dict[key][1]:
                line = '<!--%s-->' % line
            line = '  %s\n' % line
            f.write(line)
        f.write('</ResourceDictionary>\n')


if __name__ == "__main__":
    dir = os.path.dirname(os.path.abspath(__file__))
    dir = os.path.join(dir, "../OpenUtau/Strings/")
    lang_files = os.listdir(dir)
    src_file = next(filter(lambda f: f.endswith("Strings.axaml"), lang_files))
    src_file = os.path.join(dir, src_file)
    dst_files = filter(lambda f: f.endswith("axaml") and not f.endswith("Strings.axaml"), lang_files)
    dst_files = map(lambda f: os.path.join(dir, f), dst_files)

    register_all_namespaces(src_file)
    en_dict = file_to_dict(src_file)
    dict_to_file(src_file, en_dict, None)
    
    #strings unhandled by crowdin
    unhandled_strings = json.load(open(os.path.join(dir, "unhandled_strings.json"), "r", encoding='utf8'))

    for dst_file in dst_files:
        language = os.path.basename(dst_file).split('.')[1]
        dst_dict = file_to_dict(dst_file)
        for (key, item) in unhandled_strings.items():
            if(language in item["translations"]):
                dst_dict[key] = (item["tag"], item["translations"][language])
        to_remove = set(dst_dict.keys()) - set(en_dict.keys())
        to_add = set(en_dict.keys()) - set(dst_dict.keys())
        [dst_dict.pop(k) for k in to_remove]
        [dst_dict.update({k: en_dict[k]}) for k in to_add]
        dict_to_file(dst_file, dst_dict, en_dict)
