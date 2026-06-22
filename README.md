# Fortran-JSON-RPC-2.0-static-library---proof-of-concept
Implmentation of JSON RPC 2.0 protocol between a vb.net client and a Fortran server, supported by a vb.net DLL and Fortran static library

This is an attempt to provide an alternate method of transferreing data between a Fortran program and another program in another language. It utilizes Windows Named Pipes as the transfer mechanism, and the JSON Remote Procedure Call (RPC) 2.0 standard as the protocol that makes communications work between two running applications. Although the client program was written in VB.NET, it is assumed that any other language that can implement the JSON RPC protocol could potentially be used as a client for a Fortran server program.

There are four programs in the project:
1) Fortran server - a standalone Fortran program that exposes 'methods' to the client program.
2) Fortran static library - a set of code that allows for implmentation of the JSON RPC protocol for Fortran programs
3) VB.NET client - a demonstration program that implements various features and advantages of the JSON RPC protocol when working with a Fortran server
4) VB.NET DLL - a collection of helper functions; the Microsoft library StreamJsonRpc does the heavy lifting for impelmenting the JSON RPC protocol on .NET programs

Several documents are also included to provide both overview and in-depth look at the programs and how they work.
