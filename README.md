# Fortran-JSON-RPC-2.0-static-library---proof-of-concept
Implmentation of JSON RPC 2.0 protocol between a vb.net client and a Fortran server, supported by a vb.net DLL and Fortran static library

This is an attempt to provide an alternate method of transferreing data between a Fortran program and another program in another language. It utilizes Windows Named Pipes as the transfer mechanism, and the JSON Remote Procedure Call (RPC) 2.0 standard as the protocol that makes communications work between two running applications. Although the client program was written in VB.NET, it is assumed that any other language that can implement the JSON RPC protocol could potentially be used as a client for a Fortran server program.

There are four programs in the project:
1) Fortran server - a standalone Fortran program that exposes 'methods' to the client program.
2) Fortran static library - a set of code that allows for implmentation of the JSON RPC protocol for Fortran programs
3) VB.NET client - a demonstration program that implements various features and advantages of the JSON RPC protocol when working with a Fortran server
4) VB.NET DLL - a collection of helper functions; the Microsoft library StreamJsonRpc does the heavy lifting for impelmenting the JSON RPC protocol on .NET programs

Several documents are also included to provide both overview and in-depth look at the programs and how they work.

# Fortran JSON-RPC 2.0 Static Library — Proof of Concept

A proof-of-concept demonstrating bidirectional communication between a **Fortran console server** and a **VB.NET Windows Forms client** using Windows Named Pipes and the JSON-RPC 2.0 protocol.

The Fortran side is structured as two separate Visual Studio projects: a reusable static library that implements the full JSON-RPC 2.0 protocol layer, and a demonstration server application that uses it. The VB.NET side is similarly split into a reusable DLL and a demonstration client application.

---

## Table of Contents

1. [Background](#background)
2. [Key Technologies](#key-technologies)
3. [Repository Contents](#repository-contents)
4. [Prerequisites](#prerequisites)
5. [Build Order and Setup](#build-order-and-setup)
6. [Running the Demo](#running-the-demo)
7. [Implemented Methods](#implemented-methods)
8. [Architecture Notes for Developers](#architecture-notes-for-developers)
9. [Known Limitations](#known-limitations)
10. [Third-Party Libraries](#third-party-libraries)
11. [Documentation](#documentation)
12. [License](#license)

---

## Background

Fortran is still widely used for numerical computation, but its native Windows interface options are limited. VB.NET provides a full-featured Windows GUI but cannot directly call Fortran routines when complex data types, text, or runtime status updates need to be exchanged.

A common solution is to package Fortran code as a static library and call it from VB.NET via ISO_C_BINDING interop. That approach works well for simple scalar data but becomes cumbersome for strings, matrices, and anything that requires the Fortran code to send status back to the VB.NET program mid-execution (progress updates, error notifications, pause/resume signalling).

This project takes a different approach: the Fortran program runs as a **standalone executable** (not a static library) and communicates with VB.NET over a **Windows Named Pipe** using the **JSON-RPC 2.0** protocol. Both sides exchange plain text in a well-defined format, which eliminates the marshalling complexity entirely and enables true bidirectional communication, including server-initiated progress notifications during long-running computations.

---

## Key Technologies

This section briefly describes the three underpinning technologies for readers who are not familiar with inter-process communication or structured messaging.

### Windows Named Pipes

A Windows Named Pipe is an operating-system-provided communication channel identified by a human-readable name — in this project, `\\.\pipe\MyTestPipe`. One process creates the pipe (the Fortran server) and waits for a connection; the other process opens it by the same name (the VB.NET client). Once connected, both sides can send and receive data simultaneously — the pipe is full-duplex.

From a coding perspective, reading from and writing to a named pipe feels like reading from and writing to a file. Data written at one end becomes available to read at the other end in the same order it was written.

Because a named pipe delivers a continuous stream of bytes with no built-in message boundaries, this project uses an HTTP-style **Content-Length framing** scheme (borrowed from the Language Server Protocol). Before each JSON message is written to the pipe, a plain-text header of the form `Content-Length: N` followed by a blank line is prepended. The receiving side reads the header, extracts the byte count `N`, then reads exactly `N` more bytes to obtain one complete message.

### JSON

JSON (JavaScript Object Notation) is a plain-text format for representing structured data as key–value pairs and arrays. For example:

```json
{"jsonrpc": "2.0", "method": "addint", "params": [3, 5], "id": 1}
```

Despite its name, JSON is completely language-independent. Both VB.NET and Fortran can read and write it, giving the two programs a common data language regardless of their different internal type systems. JSON supports six value types: strings, numbers, booleans, null, objects (key–value maps), and arrays.

### JSON-RPC 2.0

JSON-RPC 2.0 is a lightweight remote procedure call protocol that uses JSON as its message format. It defines three message types:

| Type | Direction | Description |
|------|-----------|-------------|
| **Request** | Client → Server | Asks the server to execute a named method and return a result. Contains `jsonrpc`, `method`, `params`, and `id`. |
| **Response** | Server → Client | The server's answer. Contains the matching `id` and either a `result` field or an `error` field. |
| **Notification** | Either direction | A fire-and-forget message with no `id` field. The receiver executes the method but sends no response. Used in this project for progress updates and control signals (pause, resume, disconnect). |

JSON-RPC 2.0 is a symmetric protocol — either side can initiate messages. In this project the VB.NET client sends requests; the Fortran server sends notifications back (progress percentages, status messages) unprompted during long computations.

---

## Repository Contents

```
├── README.md                          This file
├── ReadMe notes on program compiling.txt
│
├── FORTRAN STATIC LIBRARY/            Reusable JSON-RPC 2.0 protocol library (Fortran)
│   ├── JSONRPCStaticLibraryRevA.sln   Visual Studio solution
│   ├── JSONRPCStaticLibraryRevA.vfproj  Intel Fortran project file
│   ├── fortran jsonrpc_fortran_lib.f90  JSON-RPC 2.0 protocol modules (5 modules)
│   └── ServerComm.f90                 Named pipe I/O and message dispatch module
│
├── FORTRAN SERVER/                    Demonstration Fortran server application
│   ├── FortranServerJSONRPCRevA.sln   Visual Studio solution
│   ├── FortranServerJSONRPCRevA.vfproj  Intel Fortran project file
│   ├── FortranServerJSONRPC.f90       Main program — pipe setup and method registration
│   ├── ConsoleUtils.f90               Console window helpers
│   ├── Functions.f90                  Shared state and helper routines
│   ├── Functions_ArithmeticOps.f90    Integer, real, and complex arithmetic handlers
│   ├── Functions_Benchmark.f90        Mandelbrot benchmark handler (with progress)
│   ├── Functions_MatrixComplexOps.F90 Complex matrix operation handlers
│   ├── Functions_MatrixIntegerOps.f90 Integer matrix operation handlers
│   ├── Functions_MatrixLogicalOps.F90 Logical matrix operation handlers
│   ├── Functions_MatrixRealOps.f90    Real matrix operation handlers
│   ├── Functions_MatrixTextOps.f90    Text matrix operation handlers
│   ├── Functions_StringOps.f90        String operation handlers
│   ├── JsonRpcErrorCodes.f90          Application-specific error code constants
│   ├── json-fortran.lib               Pre-compiled json-fortran library (required to build)
│   ├── libifcoremd.dll                Intel Fortran runtime DLL
│   └── libmmd.dll                     Intel Math Kernel Library runtime DLL
│
├── VB.NET DLL/                        Reusable JSON-RPC 2.0 client library (.NET)
│   ├── JSONRPCClientLibrary.sln       Visual Studio solution
│   ├── JSONRPCClientLibrary.vbproj    VB.NET project file
│   ├── AppConstants.vb                Pipe name and timeout constants
│   ├── ClientInfo.vb                  Version/metadata helpers
│   ├── ConnectionModule.vb            Named pipe connection management
│   ├── ErrorTestingModule.vb          Helpers for exercising error-path behaviour
│   ├── RPCOperations.vb               All RPC call wrappers (InvokeAsync wrappers)
│   ├── RpcTargetHandler.vb            Server-initiated notification handler
│   └── ValidationModule.vb            Result validation utilities
│
└── VB.NET CLIENT/                     Demonstration VB.NET Windows Forms client
    ├── ClientInterfaceToFortranServerRPC.sln  Visual Studio solution
    ├── ClientInterfaceToFortranServer.vbproj  VB.NET project file
    ├── AppConstants.vb                Pipe name, timeout, and method name constants
    ├── ApplicationEvents.vb           VB application startup/shutdown handler
    ├── CalculationModule.vb           Business logic for calculation operations
    ├── Form1.vb                       Main form — UI and event handlers
    ├── Form1.Designer.vb              Auto-generated form layout (keep with Form1.vb)
    ├── Form1.resx                     Form resources
    ├── MatrixModule.vb                Matrix data handling
    ├── UIHelperModule.vb              Thread-safe UI utilities
    └── ValidationModule.vb            Input validation helpers
```

### Component Roles

| Component | Role |
|-----------|------|
| **Fortran Static Library** | General-purpose, reusable. Implements JSON-RPC 2.0 parsing, validation, method dispatch, and Named Pipe I/O. Any Fortran server program can link against this library instead of implementing the protocol from scratch. |
| **Fortran Server** | Demonstration application. Registers 28 methods, handles requests from the client, and sends progress notifications. Links against the static library. |
| **VB.NET DLL** | General-purpose, reusable. Built on Microsoft's StreamJsonRpc library. Manages the named pipe connection, message framing, JSON-RPC lifecycle, and notification handling. |
| **VB.NET Client** | Demonstration Windows Forms application. Uses the DLL to invoke Fortran methods and display results. |

---

## Prerequisites

Before building, install the following:

1. **Microsoft Visual Studio 2022** (Community Edition is free)
   - Include the **.NET desktop development** workload during installation
   - Include the **Intel oneAPI toolkit** Visual Studio integration (or install separately)

2. **Intel oneAPI HPC Toolkit** — provides the **IFX** Fortran compiler
   - Download from [intel.com/content/www/us/en/developer/tools/oneapi/hpc-toolkit.html](https://www.intel.com/content/www/us/en/developer/tools/oneapi/hpc-toolkit.html)
   - The IFX compiler (64-bit) is required; the older IFORT compiler is not supported by these project files

3. **.NET 8 SDK** — typically installed with Visual Studio; verify in **Visual Studio Installer → Modify → Individual Components**

4. **StreamJsonRpc NuGet package** — referenced by both VB.NET projects. Visual Studio restores it automatically when you open and build the solution for the first time (requires internet access on first build).

---

## Build Order and Setup

The four projects must be built in order because each one produces output that the next one depends on.

```
Step 1: FORTRAN STATIC LIBRARY  →  produces JSONRPCStaticLibraryRevA.lib
Step 2: FORTRAN SERVER          →  links against JSONRPCStaticLibraryRevA.lib
Step 3: VB.NET DLL              →  produces JSONRPCClientLibrary.dll
Step 4: VB.NET CLIENT           →  links against JSONRPCClientLibrary.dll
```

### Step 1 — Fortran Static Library

1. Open `FORTRAN STATIC LIBRARY\JSONRPCStaticLibraryRevA.sln` in Visual Studio 2022.
2. The project references external `.mod` and `.lib` files in a subfolder named `AdditionalDirectories`. This subfolder must contain the pre-compiled **json-fortran** library files (`.lib` and `.mod` files). Place them there before building.
3. Select the **x64 / Debug** (or Release) configuration and build the solution.
4. The output `JSONRPCStaticLibraryRevA.lib` and a set of `.mod` files are produced in the `x64\Debug` (or `x64\Release`) output folder.

### Step 2 — Fortran Server

1. Open `FORTRAN SERVER\FortranServerJSONRPCRevA.sln` in Visual Studio 2022.
2. The project has an `AdditionalDirectories` subfolder. Copy the following into it:
   - `JSONRPCStaticLibraryRevA.lib` — the library built in Step 1
   - All `.mod` files produced by the Step 1 build (these give the server access to the library's module interfaces)
   - The json-fortran `.mod` files (same set used in Step 1)
3. The `json-fortran.lib` file is already included in the project folder and referenced directly by the project file.
4. Build using the **x64 / Debug** (or Release) configuration. The output is `FortranServerJSONRPCRevA.exe`.

> **Note on `libifcoremd.dll` and `libmmd.dll`:** These Intel Fortran runtime DLLs are included in the project folder so the server executable can run on machines that do not have the Intel Fortran runtime installed. Copy them to the same folder as the built executable if needed.

### Step 3 — VB.NET DLL

1. Open `VB.NET DLL\JSONRPCClientLibrary.sln` in Visual Studio 2022.
2. On the first build, Visual Studio will restore the **StreamJsonRpc** NuGet package automatically (internet access required).
3. Build the solution. The output is `JSONRPCClientLibrary.dll`.

### Step 4 — VB.NET Client

1. Open `VB.NET CLIENT\ClientInterfaceToFortranServerRPC.sln` in Visual Studio 2022.
2. Add a reference to `JSONRPCClientLibrary.dll` (from Step 3) if it is not already referenced, or ensure the project reference points to the correct build output path.
3. Build the solution. The output is the client executable.

---

## Running the Demo

1. **Start the Fortran server first.** Run `FortranServerJSONRPCRevA.exe`. A console window appears and the server prints its startup banner, lists all 28 registered methods, and then waits for a client connection:

   ```
   ==========================================
    Fortran JSON-RPC 2.0 Server
   ==========================================
   [1] Creating named pipe: MyTestPipe
   [SUCCESS] Named pipe created
   [2] JSON-RPC 2.0 Server started
   ...
   Waiting for client connection...
   ```

2. **Launch the VB.NET client.** The Windows Forms window (Form1) opens and automatically connects to the server over the named pipe `\\.\pipe\MyTestPipe`.

3. **Use the client** to invoke methods, observe results in the GUI, and watch the corresponding request/response exchange printed in both console windows.

4. **To stop:** click the **Close** button in the bottom-right corner of Form1 to disconnect the client gracefully. In the Fortran server console, press **Enter** when prompted to exit.

> **Tip:** Both consoles print the full JSON-RPC message exchange as it happens. This is intentional — it makes the protocol visible and is useful for learning and debugging. A production implementation would disable this output.

---

## Implemented Methods

The Fortran server registers 28 methods, all matched case-sensitively.

| Category | Method names |
|----------|-------------|
| Integer arithmetic | `addint`, `subtractint`, `multiplyint`, `divideint` |
| Real (floating-point) arithmetic | `addreal`, `subtractreal`, `multiplyreal`, `dividereal` |
| Complex number arithmetic | `addcomplex`, `subtractcomplex`, `multiplycomplex`, `dividecomplex` |
| String round-trip | `sendmessage` |
| Named parameters demo | `namedparameters` |
| Benchmark (with progress) | `mandelbrotbenchmark` |
| Integer matrix | `matrixintegertranspose`, `matrixintegercopy`, `matrixintegersquare` |
| Real matrix | `matrixrealtranspose`, `matrixrealcopy`, `matrixrealsquare` |
| Text matrix | `matrixtexttranspose`, `matrixtextcopy` |
| Logical matrix | `matrixlogicaltranspose`, `matrixlogicalcopy` |
| Complex matrix | `matrixcomplextranspose`, `matrixcomplexcopy`, `matrixcomplexsquare` |
| Notifications (no response) | `clientevent`, `disconnect` |

Complex numbers have no native JSON type. This project encodes them as strings in the form `re±imi` (e.g., `"3.5+2.1i"`). The Fortran handler parses the string, performs the arithmetic on native `COMPLEX` values, and returns the result as a string in the same format.

---

## Architecture Notes for Developers

### Static library modules

`fortran jsonrpc_fortran_lib.f90` contains five Fortran modules:

| Module | Purpose |
|--------|---------|
| `jsonrpc_types` | Derived types: `method_entry` (name + handler pointer), `json_core`, `json_value` |
| `jsonrpc_validation` | Protocol validation, error code constants, `jsonrpc_build_error_response` |
| `jsonrpc_utils` | Utility helpers including `jsonrpc_extract_json` (Content-Length framing) |
| `jsonrpc_server_state` | Shared runtime state: pipe handle and notification-mode flag, with accessors |
| `jsonrpc_protocol` | Library initialisation (`jsonrpc_init`), method registration, and dispatch |
| `jsonrpc_helpers` | Parameter extraction helpers: `jsonrpc_get_int_param`, `jsonrpc_get_string_param`, `jsonrpc_get_named_int_param`, `jsonrpc_get_named_string_param` |

`ServerComm.f90` contains the `ServerComm` module, which provides `ProcessMessages` (the main message loop), `SendErrorResponse`, and `SendProgressNotification`. It uses Windows Named Pipe I/O (`ReadFile`/`WriteFile`) and is intentionally Windows-specific; developers targeting a different transport can replace this module with their own implementation.

### Adding a new method to the Fortran server

1. Write a handler subroutine in one of the `Functions_*.f90` files following the same interface pattern as the existing handlers.
2. Add an entry to the `methods` array in `FortranServerJSONRPC.f90` and increment `NUM_METHODS`.
3. No changes to the static library are required.

### Pipe name

The pipe name `MyTestPipe` is hard-coded in both `AppConstants.vb` files (client and DLL) and in the Fortran server's main program. Change it in all three places if needed.

---

## Known Limitations

This is a Revision 0 proof-of-concept. Two intentional deviations from the JSON-RPC 2.0 specification are present:

1. **Integer-only `id` field.** The JSON-RPC 2.0 specification allows the `id` field to be an integer, a string, or null. This implementation accepts integers only. Requests with a string or null `id` are rejected with an Invalid Request error (code `-32600`). This simplification may be addressed in a future revision.

2. **Batch requests not fully compliant.** The JSON-RPC 2.0 specification requires batch requests (a JSON array of request objects) to be processed and responded to as a unit. Batch dispatch is implemented and functional, but the handling does not meet all specification requirements for Revision 0. Full compliance is planned for Revision 1.

---

## Third-Party Libraries

| Library | Language | Source | Purpose |
|---------|----------|--------|---------|
| **json-fortran** | Fortran | [github.com/jacobwilliams/json-fortran](https://github.com/jacobwilliams/json-fortran) | JSON parsing and serialisation on the Fortran side. A pre-compiled `json-fortran.lib` is included in the Fortran project folders. |
| **StreamJsonRpc** | .NET | NuGet (Microsoft) | JSON-RPC 2.0 implementation on the VB.NET side. Restored automatically by Visual Studio on first build. |

---

## Documentation

The `Documentation` folder (in the full project directory, not uploaded to this repository) contains the following reference documents:

- **Project Overview** — high-level summary and motivation
- **In Depth Program Descriptions** — detailed description of all four components and the technologies they use
- **Marshalling of Data Between Fortran and Visual Basic** — how data types are converted between JSON, Fortran, and .NET
- **Fortran Interoperability: ISO_C_BINDING vs. Windows Named Pipes + JSON-RPC 2.0** — comparison of the two interoperability approaches
- **JSON-RPC 2.0 Standard Compliance Assessment** — line-by-line review of spec conformance
- **Master List of Filenames, Modules, Subroutines, and Methods** — complete inventory of every identifier in all four projects
- **User Manual** — step-by-step guide to running the demo programs

---

## License

MIT License

Copyright (c) 2026 7L Consulting, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
