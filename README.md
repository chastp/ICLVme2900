# ICL VME 2900 Command Emulator (starter)

A .NET 8 command-level simulator for experimenting with a VME-style mainframe environment.
It is **not** a hardware emulator and does not claim exact historical VME command compatibility.
The command vocabulary is deliberately small and documented so that verified commands from
your manuals can be added without changing the kernel.

## Build and run

```powershell
cd ICLVme2900
dotnet build
dotnet run --project src/Vme2900.Console
```

The simulator creates a `data` directory under the console application's working directory.

## Initial commands

- `HELP [command]`
- `LOGON [user]` / `LOGOFF`
- `WHOAMI`, `DATE`, `TIME`, `VERSION`
- `DIR [path]`, `CD [path]`, `PWD`
- `TYPE <file>`, `CREATE <file>`, `DELETE <file>`
- `COPY <source> <destination>`, `RENAME <source> <destination>`
- `MKDIR <path>`, `RMDIR <path>`
- `SUBMIT <file>`, `JOBS`, `CANCEL <job-id>`
- `EXIT`

`CREATE` reads lines until a single `.` is entered. Paths are confined to the emulator's
virtual disk root. Commands are case-insensitive; file names are case-insensitive on lookup
while the host filesystem retains its normal behaviour.

## Design

- `Vme2900.Core`: kernel/session, command parser/registry, virtual disk and job model.
- `Vme2900.Console`: interactive terminal front end.
- Commands depend on interfaces (`IVmeFileSystem`, `IVmeJobService`) rather than the host OS.
- The virtual disk is a directory-backed development implementation. Replace it later with
  a block/file-store implementation if disk-format fidelity becomes a requirement.

## Important implementation boundary

The emulator currently models a command environment, users, a virtual directory tree and
basic batch-job records. It does not implement the 2900 instruction set, VME scheduler,
security subsystem, networking stack, terminal protocol, COBOL compiler or IDMS database.
Those are separate phases. The command names and syntax in this starter are simulator commands,
not assertions about original ICL command syntax.

## Suggested next stages

1. Collect the exact VME release and manuals; map documented commands and terminal conventions.
2. Add accounts, permissions, file attributes and catalogues.
3. Add batch job control, queues, spool files and system logs.
4. Add a COBOL runtime/compiler integration boundary.
5. Add IDMS schema, record/set services and DML runtime.
6. Consider CPU/instruction emulation only if running actual 2900 object code is a goal.
