using Vme2900.Core;

Console.Title = "ICL 2900 VME Command Emulator";
var disk = Path.Combine(AppContext.BaseDirectory, "data", "disk0");
var kernel = new VmeKernel(disk, Console.Out);

Console.WriteLine("ICL 2900 / VME command emulator");
Console.WriteLine("Simulation build 0.1 - type HELP; LOGON <user> to begin.");
Console.WriteLine();

while (true)
{
    Console.Write(kernel.Session.IsLoggedOn
        ? $"{kernel.Session.User!.UserId}:{kernel.Session.CurrentDirectory}> "
        : "VME> ");

    var line = Console.ReadLine();
    if (line is null) break;

    var result = await kernel.ExecuteLineAsync(line);
    if (!string.IsNullOrEmpty(result.Message))
        Console.WriteLine(result.Message);
    if (result.ExitRequested) break;
}
