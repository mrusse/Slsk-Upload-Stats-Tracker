# SlskQt-Upload-Stats-Tracker
A utility to track, save, and view stats on your soulseek uploads.

# ♪ Feature Showcase ♪
https://github.com/mrusse/Slsk-Upload-Stats-Tracker/assets/38119333/e8b278cb-017d-4846-afd2-4d37c337cffb

# Main stats menu
NOTE: all usernames and data in these screenshots are either randomly generated or censored for privacy

From the main menu you can see a variety of general stats such as your upload totals and top users (by download size).

![main menu](https://i.imgur.com/S11LrMI.png)

You can also view your top downloaded folders.

![main menu folder stats](https://i.imgur.com/GEi2qkj.png)

# User stats

From the user stats page you can view all the users who have downloaded from you.
For each user the program logs all their downloads and sorts them by folder.
There are a few different sorting options and a search bar. The search works on anything in the
whole database (username, folder, filename etc). You can remove any item from the database by selecting it and clicking
the "Remove Selected" button.

Anything you select in the database (users, folders, or files) will show stats and information on the selected
item in the top right. Tracked stats are things like: number of times a folder or file has been downloaded, last user to download a folder,
added up total size in kilobytes of a user's downloads, and much more.

![user stats page](https://i.imgur.com/AyAXMbJ.png)

# How to add data and start using the program

The first thing you need to do when launching the program for the first time is initialize your settings.
You need to choose a place to save the database that will be built as well as add the location
of all the folders you are sharing in soulseek. Once you have selected your desired save location
and added all the same folders that you are sharing in soulseek you can clik the "Save settings" button.
Here's an example of my settings page.

![settings page](https://i.imgur.com/GTWwqzn.png)

Now for adding some data. You first need to enable "Show diagnostics" in the SoulseekQT settings
under "Options" > "UI"

![slsk settings](https://i.imgur.com/jevgodt.png)

Then you can naviagte to "Diagnostics" > "Logs" > "Transfer Queue". This is where you can copy the data to paste into
the stats tracker. NOTE: You will need to copy this data before closing soulseek as the SoulseekQT client
does not keep the data after closing. You can also copy the data anytime you have a new upload. The stats tracking program
will only gather new data.

Simply select all with "Ctrl-A" and copy with "Ctrl-C"

![slsk settings](https://i.imgur.com/xmqBJsW.png)

You can then paste this data into the "Input" text box on the "Data Input" tab of the stats tracking program.

![slsk settings](https://i.imgur.com/i0PjbCz.png)

Once the data has been pasted you can click the "Submit to database" button and it will find any new upload data that is not already in your database.

![slsk settings](https://i.imgur.com/L8VLpGw.png)

After adding new data it will be stored permanently in the stats tracking program. You don't need to worry about losing your upload data.

