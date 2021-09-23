#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "System"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"
#load "findCoin.fsx"
// #time "on"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FindCoin
open System.Diagnostics

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
                    port = 9003
                    hostname = 127.0.0.1
                }
            }
        }")
// 10.136.28.175
type MessageSystem =
    | TransitMsg of int * string * string * int * int

let actor_num = 100
let mutable coin_count = 0
let system = System.create "Server" configuration
let mutable nn = 0

let proc = Process.GetCurrentProcess()
let cpu_time_stamp = proc.TotalProcessorTime
let timer = new Stopwatch()
timer.Start()


let myActor (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! TransitMsg(n, content, param, num, protedIndex) = mailbox.Receive()
        let sender = mailbox.Sender()
        let mutable s = "0"
        match content with
        | "go to work" -> printfn "local actor %d start to work" n ; s <- FindCoin.findCoin(param, num, protedIndex) 
        | _ -> printfn "actor don't understand"
        let returnMsg = sprintf "bitcoin;%d;%s;%d" n s protedIndex
        sender <! returnMsg
        return! loop()
    }
    loop()

// let clientRegister addr, addrArray refArray= 

let myMonitor (mailbox: Actor<string>) =
    let mutable actorCountNum = 0
    let mutable actorAppendNum = 0
    let mutable actori = 0
    let mutable zeroNum = 0
    let mutable protectedIndex = 0
    // let mutable zeroNumArray = Array.create actor_num 0

    let actorArray = Array.create actor_num (spawn system "myActor" myActor)
    {0..actor_num-1} |> Seq.iter (fun a ->
        actorArray.[a] <- spawn system (string a) myActor
        ()
    )
    // let serverRef = select (parseMsg.[1]) myMonitor
    let mutable clientAddArray = [||]
    let mutable clientRefs = [||]
    let mutable client_count = 0
    

    let rec loop() =
        actor {
            let! msg = mailbox.Receive()
            // let sender = mailbox.Sender()

            let parseMsg = msg.Split ';'
            
            // "content;id/addr;paramter"
            match parseMsg.[0] with
            | "register" -> let mutable register_already = false
                            for i in 0..client_count-1 do
                                if parseMsg.[1] = clientAddArray.[i] then
                                    register_already <- true
                                    clientRefs.[i] <- select (parseMsg.[1]) system
                            if not register_already then
                                clientAddArray  <- [|parseMsg.[1]|] |> Array.append clientAddArray;
                                clientRefs  <- [|select (parseMsg.[1]) system|] |> Array.append clientRefs;
                                client_count <- client_count+1;
                            let s = parseMsg.[2] + System.Text.Encoding.ASCII.GetString( [|byte(0x20 + actorCountNum)|])
                            let cMsg = sprintf "go to work; ;%s;%d;%d" parseMsg.[2] zeroNum protectedIndex//new task
                            clientRefs.[client_count-1] <! cMsg
                            printfn "Count %d, Welcome: %s" client_count parseMsg.[1];

            | "start" -> actorAppendNum <- int(parseMsg.[2]);{actorCountNum..actorCountNum+actorAppendNum-1} |> Seq.iter(fun a ->
                        // zeroNumArray.[a] <- int(parseMsg.[4])
                        zeroNum <- int(parseMsg.[4])
                        let s = parseMsg.[3] + System.Text.Encoding.ASCII.GetString( [|byte(0x20 + a)|])
                        protectedIndex <- s.Length
                        actorArray.[a] <! TransitMsg(a, "go to work", s, zeroNum, protectedIndex)
                        ()
                            );actorAppendNum <- actorCountNum+actorAppendNum
                      

            // | "find nothing" -> actori <- int(parseMsg.[1]); 
            //                     actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2]);
  
            | "bitcoin" ->  printfn "bitcoin: %s" parseMsg.[2];
                            actori <- int(parseMsg.[1]);
                            let strTemp = FindCoin.increaseString(parseMsg.[2])
                            actorArray.[actori] <! TransitMsg(actori, "go to work", strTemp, zeroNum, protectedIndex);
            
            | "client bitcoin" -> printfn "client bitcoin: %s" parseMsg.[2];
                                  for i in 0..client_count-1 do
                                    if clientAddArray.[i] = parseMsg.[1] then
                                        let tempStr = FindCoin.increaseString(parseMsg.[2])
                                        let cMsg = sprintf "go to work; ;%s;%d;%d"  tempStr zeroNum protectedIndex
                                        clientRefs.[i] <! cMsg
                                  coin_count <- coin_count+1

            | _ -> printfn "manager doesn't understand"
            let cpu_time = (proc.TotalProcessorTime-cpu_time_stamp).TotalMilliseconds
            printfn "CPU time = %dms  Absolute time =%dms" (int64 cpu_time) timer.ElapsedMilliseconds
            return! loop()
        }
    loop()

let serverRef = spawn system "server" myMonitor
printfn "server initial"
serverRef <! "start;null;3;xiongruoyang;6";;
System.Console.ReadLine() |> ignore
