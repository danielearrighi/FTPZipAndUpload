# FTPZipAndUpload
A simple console program I created for backing up my website folders in my Webserver every week (using a scheduled task). It creates a zip file for every sub-directory in a specified directory (like C:\wwwroot) and uploads the files to a remote FTP server and folder. It also cleans directories older than a predefined amount of days.

This is a very simple program written for personal use so it missings checks and maybe some extra functionality.

I just wanted to share this if someone needs it.

## Usage ##

FTP Server IP, User and Password must always be specified in the App.Config file.

    FTPZipAndUpload.exe -i "C:\Test" -o "C:\Temp" -f "Test/"

This will backup all sub-directories inside C:\Test directory (putting temporary zip files in C:\Temp directory) and uploads the zips in the Test/ directory in the FTP sever.

**Useful Commands**

- -v for verbose output in the console. Default is false.
- -p "password" to get the encrypted version of the password to use to connect to FTP (to be saved in App.Config).