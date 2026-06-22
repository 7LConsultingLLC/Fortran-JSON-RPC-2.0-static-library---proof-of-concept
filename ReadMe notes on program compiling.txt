BUILD ORDER FOR PROGRAMS:

1. FORTAN STATIC LIBRARY > JSONRPCStaticLibraryRevA.lib
2. FORTRAN SERVER > links against JSONRPCStaticLibraryRevA.lib
3. VB.NET DLL > JSONRPCClientLibrary.dll
4. VB.NET CLIENT > links against JSONRPCClientLibrary.dll

Note also when compiling the Fortran static library and server program using Intel Fortran IFX on Visual Studio, the .mod and .lib files associated with each supporting *.f90 file must be in a folder such as AdditionalDirectories such that the compiler can find and link with the files when compiling the library or server program