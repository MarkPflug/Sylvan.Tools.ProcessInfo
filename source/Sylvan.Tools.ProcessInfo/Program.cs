using System.Diagnostics;

Process proc;

// wall clock
var wall = Stopwatch.StartNew();

if (args.Length == 1 && int.TryParse(args[0], out int processId))
{
    try
    {
        proc = Process.GetProcessById(processId);
    }
    catch
    {
        Console.Error.WriteLine("No process with pid #{processId}");
        return 1;
    }
}
else
{
	var name = args[0];
    var psi = new ProcessStartInfo()
    {
        UseShellExecute = false,
        FileName = name,
        Arguments = string.Join(' ', args.Skip(1).ToArray()),
    };
	try
	{
		proc = Process.Start(psi)!;
		// bit confused by the null return here. I would have assumed non-null return or exception.
		if (proc == null)
			throw new Exception();
	}
	catch
	{
		Console.Error.WriteLine($"Could not start process with name {name}.");
		return 2;
	}
}

var info = new Info(proc);

while (!proc.WaitForExit(100))
{
    info.Update(proc);
}
wall.Stop();

WriteStatus(info, wall.Elapsed);
Console.WriteLine("Process exited: " + proc.ExitCode);
return 0;

static void WriteStatus(Info info, TimeSpan wall)
{
    Console.WriteLine("Name: " + info.Name);
    Console.WriteLine("Wall Clock: " + wall);
    Console.WriteLine("CPU Total: " + (info.UserTime + info.SysTime));
    Console.WriteLine("CPU User: " + info.UserTime);
    Console.WriteLine("CPU System: " + info.SysTime);
    Console.WriteLine("Memory: " + info.Memory);
}

class Info
{
    public string Name { get; }
    public long Memory { get; private set; }
    public TimeSpan UserTime { get; private set; }
    public TimeSpan SysTime { get; private set; }

    public Info(Process proc)
    {
        this.Name = proc.ProcessName;
        Update(proc);
    }

    public void Update(Process proc)
    {
        if (!proc.HasExited)
        {
            try
            {
                // this will fail if the process has already terminated.
                proc.Refresh();
                this.Memory = proc.PeakWorkingSet64;
                this.UserTime = proc.UserProcessorTime;
                this.SysTime = proc.PrivilegedProcessorTime;
            }
            catch { }
        }
    }
}

