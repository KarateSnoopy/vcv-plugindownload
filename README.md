# vcv-plugindownload

Simple VCV rack plugin zip file downloader.
It grabs the list of plugins from https://github.com/VCVRack/community/tree/master/plugins, and downloads all the Windows zip files that aren't already downloaded.
It is simple and ignores SHA and version numbers and skips any zip that it can't download due to broken links.

Here's an example:
![Example](Example.PNG?raw=true "Example")

# Support for other platforms
If a dev wants to make it build on Mac or Linux, please feel free and send a pull request.
