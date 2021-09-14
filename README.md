Simple utility to divide a domain into separate meshes; to help only with running cases faster on high CPU machines. 

It will attempt to solve multi-mesh cases with different cell sizes, but may not work on complex set ups.  

There are two exe files.  The smaller one assumes your local PC has .net6 installed.  The largest one comes with .net6 components, which is why it is larger.

To run:


1. Move either of the exe files into a directory containing a *.fds file.
2. Run the application, and define how many meshes you want.  This should be more than the number in the current file.
3. The application will create a new *_MESHNUMBER_Meshes.fds file with the new meshes included.  The original file is not modified.

You can also use the command prompt.  Open a command prompt and navigate to the fds directory when the application is.  Start typing the name of the app. Use TAB to automatically find the .exe.  Then either:

3a. Enter Number of Meshes, i.e.
"FDS_MeshSplitter .net6 IS included.exe" 11

3b. Enter File Name (again use TAB) followed by number of meshes, i.e.
"FDS_MeshSplitter .net6 IS included.exe" Box.fds 22

A new file call Box_11_Meshes.fds is created. 

If it doesn’t work please let me know at matt@salisburyfire.co.uk