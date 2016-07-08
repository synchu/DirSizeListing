# DirSizeListing
Windows Forms project. Displays a selected Windows Folder contents together with folder's sizes.

## Description
With so many new technologies to try my HDD quickly packs up (you gotta love npm install, but it eats space for breakfast quicker 
than a bunch of hungry sailors  ;) ). 

Working with typical disk cleanups requires more intelligence that I am able to spare, hence I quickly did this very simple utility
(Windows only - ver.7 and above), 
showing a simple list of contents of a selected folder comprising type, name and size (in KB) of each item. 

It scans subfolders for each folder in the list and computes the overall size (of files, not disk space allocated - didn't bother 
to find each file disk allocation blocks count and calculate the size on disk - as it doesn't realy matter for the purposes of this utility).

A double click on a folder opens it within Windows Explorer, allowing to do whatever you want to do there (like backup/clean/etc.).

Please, note that if file/folder names exceed 256 characters in length or you don't have permissions to access given file/folder, it is
not added to the displayed size. 

Also, I haven't had the opportunity to test it on network files/folders.

The user interface should be rather self-explanatory.

Feel free to comment/contribute.

## Building the solution
Simply clone the repository, then open and build the solution in Visual Studio.

## More
Folder enumeration uses .NET Task class to run in a separate thread, allowing for fast enumeration/size calculation, showing progress bar and navigating through the currently displayed list of items at the same time.

