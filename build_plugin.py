import os
import sys
import argparse
import os.path

parser = argparse.ArgumentParser()
parser.add_argument('--version', required=True)
parser.add_argument('--prerelease')
opts = parser.parse_args()

version = opts.version
prerelease = bool(opts.prerelease)
tag = os.popen('git describe --abbrev=0').read().strip()
changelog = os.popen("git tag -l --format='%(contents)' " + tag).read().strip()

artifact_dir = os.path.join(os.getcwd(), 'artifacts')
os.mkdir(artifact_dir)


git_version = version
# .NET dll need major.minor[.build[.revision]] version format
if version.startswith('v'):
    version = version.lstrip("v")
version_list = version.split('.')
if len(version_list) == 3:
    version_list.append('0')
version = '.'.join(version_list)


if prerelease:
    jellyfin_repo_file = "./manifest-unstable.json"
    jellyfin_old_manifest = "https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download/manifest/manifest-unstable.json"
else:
    jellyfin_repo_file = "./manifest.json"
    jellyfin_old_manifest = "https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download/manifest/manifest.json"

# download old manifest
jellyfin_manifest_template = "./doc/manifest-template.json"
os.system('wget -q -O "%s" "%s" ' % (jellyfin_repo_file, jellyfin_old_manifest))
if not os.path.isfile(jellyfin_repo_file):
    os.system('cp -f %s %s' % (jellyfin_manifest_template, jellyfin_repo_file))


# update change log
build_yaml_file = "./build.yaml"
with open(build_yaml_file, 'r') as file:
    data = file.read()
    data = data.replace("NA", changelog)
with open(build_yaml_file, 'w') as file:
    file.write(data)


# build and generate new manifest
jellyfin_repo_url = "https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download"

zipfile = os.popen('jprm --verbosity=debug plugin build "." --output="%s" --version="%s" --dotnet-framework="net6.0"' %
                   (artifact_dir, version)).read().strip()

os.system('jprm repo add --url=%s %s %s' % (jellyfin_repo_url, jellyfin_repo_file, zipfile))

os.system('sed -i "s/\/danmu\//\/%s\//" %s' % (git_version, jellyfin_repo_file))


# 国内加速
jellyfin_repo_file_cn = jellyfin_repo_file.replace(".json", "_cn.json")
os.system('cp -f %s %s' % (jellyfin_repo_file, jellyfin_repo_file_cn))
os.system('sed -i "s/github.com/ghproxy.com\/https:\/\/github.com/g" "%s"' % (jellyfin_repo_file_cn))


print(version)
