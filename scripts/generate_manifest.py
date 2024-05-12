#!/usr/bin/env python3
import hashlib
import json
import sys
import re
import os
import subprocess
from datetime import datetime
from urllib.request import urlopen
from urllib.error import HTTPError


def generate_manifest():
    return    [{
        "guid": "5B39DA44-5314-4940-8E26-54C821C17F86",
        "name": "Danmu",
        "description": "jellyfin的b站弹幕自动下载插件，会匹配b站番剧/电影视频，自动下载对应弹幕，并定时更新。",
        "overview": "jellyfin弹幕下载插件",
        "owner": "cxfksword",
        "category": "Metadata",
        "imageUrl": "https://github.com/cxfksword/jellyfin-plugin-danmu/raw/main/doc/logo.png",
        "versions": []
    }]

def generate_version(filepath, version, changelog):
    return {
        'version': f"{version}.0",
        'changelog': changelog,
        'targetAbi': '10.9.0.0',
        'sourceUrl': f'https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download/v{version}/danmu_{version}.0.zip',
        'checksum': md5sum(filepath),
        'timestamp': datetime.now().strftime('%Y-%m-%dT%H:%M:%S')
    }

def md5sum(filename):
    with open(filename, 'rb') as f:
        return hashlib.md5(f.read()).hexdigest()


def main():
    filename = sys.argv[1]
    tag = sys.argv[2]
    version = tag.lstrip('v')
    filepath = os.path.join(os.getcwd(), filename)
    result = subprocess.run(['git', 'tag','-l','--format=%(contents)', tag, '-l'], stdout=subprocess.PIPE)
    changelog = result.stdout.decode('utf-8').strip()

    # 解析旧 manifest
    try:
        with urlopen('https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download/manifest/manifest.json') as f:
            manifest = json.load(f)
    except HTTPError as err:
        if err.code == 404:
            manifest = generate_manifest()
        else:
            raise

    # 追加新版本/覆盖旧版本
    manifest[0]['versions'] = list(filter(lambda x: x['version'] != f"{version}.0", manifest[0]['versions']))
    manifest[0]['versions'].insert(0, generate_version(filepath, version, changelog))

    with open('manifest.json', 'w') as f:
        json.dump(manifest, f, indent=2)

    # 国内加速
    cn_domain = 'https://mirror.ghproxy.com/'
    if 'CN_DOMAIN' in os.environ and os.environ["CN_DOMAIN"]:
        cn_domain = os.environ["CN_DOMAIN"]
    cn_domain = cn_domain.rstrip('/')
    with open('manifest_cn.json', 'w') as f:
        manifest_cn = json.dumps(manifest, indent=2)
        manifest_cn = re.sub('https://github.com', f'{cn_domain}/https://github.com', manifest_cn)
        f.write(manifest_cn)


if __name__ == '__main__':
    main()