#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "System"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"
#load "FindCoin.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FindCoin
open System.Diagnostics
open System.Security.Cryptography

let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = 100
                    hostname = 127.0.0.1
                }
            }
        }")
// 10.136.28.175
type MessageSystem =
    | TransitMsg of int * string * string * int * int

let actor_num = 200
let mutable coin_count = 0
let system = System.create "Server" configuration
let mutable nn = 0



let stringToHashHex(s:string) = 
                        let hashHex =  SHA256.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(s));
                        ConcatArray hashHex

let parseBitcoin(s:string) =    let mutable str = s
                                for i in 1..3 do
                                    str <- str.[str.IndexOf(";")+1..]
                                str

let myActor (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! TransitMsg(n, content, param, num, protedIndex) = mailbox.Receive()
        let sender = mailbox.Sender()
        let mutable s = ""
        match content with
        | "go to work" -> s <- FindCoin.findCoin(param, num, protedIndex);
                        // printfn "local actor %d start to work" n
        | _ -> printfn "actor don't understand"
        let returnMsg = sprintf "bitcoin;%d;%d;%s" n protedIndex s
        sender <! returnMsg
        return! loop()
    }
    loop()

let myMonitor (mailbox: Actor<string>) =
    let mutable actorCountNum = 0
    let mutable actorAppendNum = 0
    let mutable actori = 0
    let mutable zeroNum = 0
    let mutable protectedIndex = 0

    let actorArray = Array.create actor_num (spawn system "myActor" myActor)
    {0..actor_num-1} |> Seq.iter (fun a ->
        actorArray.[a] <- spawn system (string a) myActor
        ()
    )
    let mutable clientAddArray = [||]
    let mutable clientRefs = [||]
    let mutable client_count = 0
    let mutable prefix = ""
    let proc = Process.GetCurrentProcess()
    let cpu_time_stamp = proc.TotalProcessorTime
    let timer = new Stopwatch()
    timer.Start()

    let rec loop() =
        actor {
            let! msg = mailbox.Receive()
            let parseMsg = msg.Split ';'
            match parseMsg.[0] with
            // Process client registeration. If client has not been registered, create a new bucket for it
            // If the connnection has been estibalished before, replace original connection by a new connection.

            | "register" -> let mutable register_already = false
                            let mutable cur_client = -1
                            for i in 0..client_count-1 do
                                if parseMsg.[1] = clientAddArray.[i] then
                                    register_already <- true
                                    clientRefs.[i] <- select (parseMsg.[1]) system
                                    cur_client <- i
                                    
                            if not register_already then
                                clientAddArray  <- [|parseMsg.[1]|] |> Array.append clientAddArray;
                                clientRefs  <- [|select (parseMsg.[1]) system|] |> Array.append clientRefs;
                                cur_client <- client_count
                                client_count <- client_count+1;
                            printfn "%s" prefix
                            let s = prefix + System.Text.Encoding.ASCII.GetString( [|byte(0x20 + actorCountNum)|])
                            let cMsg = sprintf "go to work;%d;%d;%s" zeroNum protectedIndex s //new task
                            clientRefs.[cur_client] <! cMsg
                            printfn "Count %d, Welcome: %s" client_count parseMsg.[1];
                            actorCountNum <- actorCountNum + 1

            // If the server receive "start" command from the user, server begins to work.
            | "start" ->prefix <- parseMsg.[3]
                        protectedIndex <- prefix.Length + 1
                        zeroNum <- int(parseMsg.[4])

                        actorAppendNum <- int(parseMsg.[2]);{actorCountNum..actorCountNum+actorAppendNum-1} |> Seq.iter(fun a ->
                        let s = parseMsg.[3] + System.Text.Encoding.ASCII.GetString( [|byte(0x20 + a)|])
                        prefix <- parseMsg.[3]
                        protectedIndex <- s.Length
                        actorArray.[a] <! TransitMsg(a, "go to work", s, zeroNum, protectedIndex)
                        ()
                            );actorCountNum <- actorCountNum+actorAppendNum
  
            | "bitcoin" ->  coin_count <- coin_count+1
                            let bincoin = parseBitcoin(msg)
                            let hash = stringToHashHex(bincoin)
                            printfn "%s\t%s" bincoin hash

                            actori <- int(parseMsg.[1]);
                            let strTemp = FindCoin.increaseString(bincoin)
                            actorArray.[actori] <! TransitMsg(actori, "go to work", strTemp, zeroNum, protectedIndex);
                            
            
            | "bitcoin client" ->coin_count <- coin_count+1
                                 let bincoin = parseBitcoin(msg)
                                 let hash = stringToHashHex(bincoin)
                                 printfn "%s\t%s" bincoin hash
                                 
                                 for i in 0..client_count-1 do
                                    if clientAddArray.[i] = parseMsg.[1] then
                                        let tempStr = FindCoin.increaseString(bincoin)
                                        let cMsg = sprintf "go to work;%d;%d;%s"  zeroNum protectedIndex tempStr
                                        // printfn "send %s" cMsg 
                                        clientRefs.[i] <! cMsg
                                  

            | _ -> printfn "manager doesn't understand"

            // Measure CPU time and the real time
            if coin_count%10 = 0 then 
                let cpu_time = (proc.TotalProcessorTime-cpu_time_stamp).TotalMilliseconds
                let elapse = timer.ElapsedMilliseconds
                printfn "CPU time = %d ms    Real time = %d ms   CPU time/Real time = %f" (int64 cpu_time) elapse (float(cpu_time)/float(elapse))
            return! loop()
        }
    loop()

let serverRef = spawn system "server" myMonitor
printfn "Server init"
let input(n) = let mutable str = "start;null;3;xiongruoyang;" //"command to monitor;null;number of workrs;prefix;"
               str <- str + string(n)
               serverRef <! str;;

input(7) // n means the number of leading 0's
System.Console.ReadLine() |> ignore
