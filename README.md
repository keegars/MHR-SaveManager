# MHR- Save Manager

Initial Release:

- Includes checks for steam running
- Includes checks for only one running version of program
- Includes checks for current logged in steam user (only to get userid)
- Creates bat file in folder, to navigate quickly to save data folder for game
- Checks if game is already running, and whether user wants to kill process and launch game or skip the launch process
- Auto runs game via the steam api
- Auto detects the MH Rise injector if placed within same directory, only need to run this to start the injector along side. (One application to run rise)
- Puts the program automatically to below normal thread priority to minimise performance impact (next to no performance loss noticed whilst using personally over the last 30 days)
- Schedules the save data to be backed up every 15 minutes, and keeps a running total of 100 back ups, with the oldest being removed to keep it to the total 100. This gives roughly the last 25 hours of game time worth of saves, but do note that a back is made when first ran so it is only an estimation. Please be aware, that this folder can get large quick (~1GB with all 100 backups, depending on save folder size being roughly 10MB each time)
- Detects if game has exited, and if so will close the save manager (this works most of the time, however if the game process lingers after it has exited, as is the game sometimes with ReShade not closing it properly, you will still need to press Enter on the console screen.)
- Errors will be logged to an errorlog.txt for ease of troubleshooting
- All saves will be stored in a folder named "Save Backup Folder" under the naming convention of year-month-day_hour_minutes_seconds this is to make looking for the latest save easier, and can be sorted by ascending/descending on the name field on windows.

# Usage:

1. Place into a folder on it's own, or along side the MH reshade injector
2. Run the MHR-SaveManager.exe
3. As long as steam, and a user are logged in, it will attempt to start the game, or if detected run the reshade injector to start the game with ReShade.
4. Play the game!
5. Once the game has exited, press ENTER key on the console and it will close down within 5 seconds (this is to give the process time to finish any copying, and to minimise cpu usage time)

# Restoring save data:

1. Navigate to the Save Backup Folder
2. Select which save data folder you'd like to use
3. Run the .bat that was generated in the same folder as the MHR-SaveManager to navigate to the current save data folder for the game
4. This next step will REMOVE your current save data and replace it, so do be careful if you do not wish to do this.
	4.1 Copy the contents of the save data folder you are wanting to replace you data, and copy/paste it into the save data location that
        opened on step 3, say yes to any files it is try to replace.
5. Run the game via the MHR-SaveManager and your cloud saves will sync (if you have this enabled on steam) and you should have your previous save back.
    Do note, I've noticed that with other games the Steam Cloud backup can restore old saves at times. But with Rise, it seems this isn't an issue.

Do note, whilst this reads the registry, it will only ever READ from it. No modifying is done, and no destructive actions apart from the removal of the oldest backup is performed.

# Ini Settings Usage
1. Run the program at least once
2. You will find a new file named MHR-SaveManage.ini
3. Open with notepad or notepad++
4. Change the values as needed

Each option:
MaxSaves - int - 1-2147483647 - default 100
* This controls how many max saves will be held

BackupInterval - int - 1-2147483647 - default 15
* This controls how often a backup is done in minutes

EnableAutoGameLaunch - boolean - True/False - default True
* This controls whether the program will auto launch the game when running, this can be set to False to disable all the auto launch features and be solely used to only back up the saves.

UseAlternativeLaunchExecutable - boolean - True/False - default False
* This controls whether the program will use the AtlernativeLaunchExecutable variable to launch the game instead e.g. with hunterpie or another program altogether.
* NOTE - If EnableAutoGameLaunch is False, this option will be disabled

AlternativeLaunchExecutable - string - any characters - default Empty
* This holds the location of an alternative executable to launch instead of launching through steam or the ReShade Injector. Can be used by giving the FULL path including executable name e.g. 
AlternativeLaunchExecutable=D:\Games\ReShade\injector.exe 
to launch using an executable.

IgnoreSS_data_slot_bin_Files - boolean - True/False - default False
* This controls whether during the back up process the files that match the format SSX_dataXXXSlot.bin will be ignored. By default this is off, only turn on if you know what you are doing!!!
