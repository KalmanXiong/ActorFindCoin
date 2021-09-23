open System.Diagnostics

let proc = Process.GetCurrentProcess()
let cpu_time_stamp = proc.TotalProcessorTime
let timer = new Stopwatch()
timer.Start()

let mutable my_array = [||]
for i in 1 .. 50000 do
    my_array  <- [|string(i)|] |> Array.append my_array
    
// printfn "my array is: %A" my_array

let cpu_time = (proc.TotalProcessorTime-cpu_time_stamp).TotalMilliseconds
printfn "CPU time = %dms" (int64 cpu_time)
printfn "Absolute time = %dms" timer.ElapsedMilliseconds

