let mutable my_array = [||]
for i in 1 .. 0 do
    my_array  <- [|string(i)|] |> Array.append my_array 
printfn "my array is: %A" my_array