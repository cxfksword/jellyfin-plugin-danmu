import os
import sys
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('--version', required=True)
parser.add_argument('--prerelease')
opts = parser.parse_args()

version = opts.version
prerelease = bool(opts.prerelease)

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
else:
    jellyfin_repo_file = "./manifest.json"

jellyfin_repo_url = "https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download/%s" % (git_version)

zipfile = os.popen('jprm --verbosity=debug plugin build "." --output="%s" --version="%s" --dotnet-framework="net6.0"' %
                   (artifact_dir, version)).read().strip()

os.system('jprm repo add --url=%s %s %s' % (jellyfin_repo_url, jellyfin_repo_file, zipfile))

print(version)
