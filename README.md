Simple utility to divide a domain into separate meshes; to help only with running cases faster on high CPU machines. 

It will attempt to solve multi-mesh cases with different cell sizes, but may not work on complex set ups.  

Open Folder: bin/Release/net6.0/publish/win-x86
There are two exe files.  The smaller one assumes your local PC has .net6 installed.  The largest one comes with .net6 components, which is why it is larger. Save them both to your local computer. 

To run:

0.	Move either of the exe files into a directory containing a *.fds file.

1.	Run the application, and define how many meshes you want.  This should be more than the number in the current file.
	
	The application will create a new *_MESHNUMBER_Meshes.fds file with the new meshes included.  The original file is not modified.  It automatically find the first FDS file in the folder, and will only work on the first one found.

2.	Run the application via the command prompt.  

	Open a command prompt and navigate to the fds directory where the application is saved.  Start typing the name of the app. Use TAB to automatically find the .exe.  Then either:

2a.	Enter Number of Meshes, i.e.
	"FDS_MeshSplitter .net6 IS included.exe" 11
	
	A new file call Box_11_Meshes.fds is created in the same directory. 

2b.	Enter File Name (again use TAB) followed by number of meshes, i.e.
	"FDS_MeshSplitter .net6 IS included.exe" Box.fds 22

	A new file call Box_22_Meshes.fds is created in the same directory. 

If it doesn’t work please let me know at matt@salisburyfire.co.uk