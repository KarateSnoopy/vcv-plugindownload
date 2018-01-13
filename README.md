# vcv-plugindownload

Given that the Rack plugin manager is disabled for understandable reasons, I made a simple command line VCV rack plugin zip file downloader.  It grabs the list of plugins from https://github.com/VCVRack/community/tree/master/plugins, and downloads all the Windows zip files that aren't already downloaded. It is simple and ignores SHA and version numbers and skips any zip that it can't download due to broken links.  It doesn't exact the zips or anything fancy, but it should be easy to just run it occasionally to have it fetch any new or updated plugin since you last ran it assuming you just keep the zips it downloads.

Here's an example:
![Example](Example.PNG?raw=true "Example")

# Support for other platforms
If a dev wants to make it build on Mac or Linux, please feel free and send a pull request.
