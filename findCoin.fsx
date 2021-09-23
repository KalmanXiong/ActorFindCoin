//module FindCoin
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "System"
open System
open System.IO
open System.Security.Cryptography
//open Akka.FSharp
//open Akka.Actor

let rec increaseBytes (index, bytesArray:byte[]) : byte[] = 
    let mutable bytes = bytesArray
    while index > (bytes.Length- 1) do
        bytes <- Array.append bytes [|0x20uy|]
        //bytesArray
    if index = 0 then
        bytes <- Array.append bytes [|0x20uy|]
        increaseBytes (bytes.Length - 1, bytes)
    elif bytes.[index] = 0x7euy then
        bytes.[index] <- 0x20uy
        increaseBytes (index - 1, bytes)
    else
        bytes.[index] <-(bytes.[index] + 1uy)
        //printfn(System.Text.Encoding.ASCII.GetString(bytes))
        bytes

let increaseString (s:string) = 
    // printfn "intput string : %s, the lenth : %d" s s.Length
    let a:byte[] = System.Text.Encoding.ASCII.GetBytes(s:string)
    let s2 = System.Text.Encoding.ASCII.GetString(increaseBytes(a.Length - 1, a))
    //printfn "output string : %s, the lenth : %d" s2 s2.Length
    s2

let juedgeBytes (bytes:byte[], num:int) =
    if num < 1 then
        false
    else
        let hashValue = SHA256.Create().ComputeHash(bytes)
        let mutable isGood = true
        let m = num / 2
        let n = num % 2
        let mutable count = 0
        while count < m && isGood do
            if hashValue.[count] <> 0uy then
                isGood <- false
            else
                count <- count + 1
        if isGood && n <> 0 then
            isGood <- hashValue.[m] < 0x10uy
        isGood

let ByteToHex bytes = 
    bytes 
    |> Array.map (fun (x : byte) -> String.Format("{0:X2}", x))

let ConcatArray stringArray = String.Join(null, (ByteToHex  stringArray))

let findCoin (s:string, num:int, protectedIndex:int) =
    let mutable isFindCoin = false
    let srcBytes = System.Text.Encoding.ASCII.GetBytes(s)
    let mutable proBytes = [|0x20uy|]
    let mutable bytes = [|0x20uy|]
    let mutable  bindBytes = [|0x20uy|]
    if protectedIndex <= 0 then
        proBytes <- Array.sub srcBytes 0 0
        bytes <- Array.sub srcBytes 0 srcBytes.Length
    elif srcBytes.Length <= protectedIndex then
        proBytes <- srcBytes
    else 
        proBytes <- Array.sub srcBytes 0 protectedIndex
        bytes <- Array.sub srcBytes protectedIndex (srcBytes.Length - protectedIndex)
    let tempStr = System.Text.Encoding.ASCII.GetString(proBytes)
    // printfn "Let's find coins! The protectedString is %s, The num is %d" tempStr num
    
    while not isFindCoin do
        bindBytes <- Array.append proBytes bytes
        isFindCoin <- juedgeBytes(bindBytes, num)
        if isFindCoin then
            // let str = System.Text.Encoding.ASCII.GetString bindBytes
            // let strHex = ConcatArray bindBytes
            // let hashBytes = SHA256.Create().ComputeHash(bindBytes)
            // let hashStr = ConcatArray hashBytes
            // printfn "Find coin! The coin is %s, The size of str is %d, The hex of str is %s, The hash hex is %s" str str.Length strHex hashStr
            // printfn "Find coin!"
            isFindCoin <- isFindCoin
        else
            //printfn "Don't find coin,The pre is %s" s
            bytes <- increaseBytes(bytes.Length - 1, bytes)
            // isFindCoin <- isFindCoin
    System.Text.Encoding.ASCII.GetString bindBytes