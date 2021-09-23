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
    | TransitMsg of int * string * string * int

let actor_num = 100
let mutable coin_count = 0
let system = System.create "Server" configuration
let mutable nn = 0

let myActor (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! TransitMsg(n, content, param, num) = mailbox.Receive()
        let sender = mailbox.Sender()
        let mutable s = "0"
        match content with
        | "go to work" -> printfn "local actor %d start to work" n ; s <- FindCoin.findCoin(param, num)
        | _ -> printfn "actor don't understand"
        let returnMsg = sprintf "bitcoin;%d;%s;" n s
        sender <! returnMsg
        // if coin_count<10 then   
        //         return! loop()
        //     else
        //         printfn "---return---"
        return! loop()
    }
    loop()

// let clientRegister addr, addrArray refArray= 

let myMonitor (mailbox: Actor<string>) =
    let mutable actorCountNum = 0
    let mutable actorAppendNum = 0
    let mutable actori = 0
    let mutable zeroNum = 0
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
                            let cMsg = sprintf "go to work; ;%s;%d" parseMsg.[2] zeroNum//new task
                            clientRefs.[client_count-1] <! cMsg
                            printfn "Count %d, Welcome: %s" client_count parseMsg.[1];

            | "start" -> actorAppendNum <- int(parseMsg.[2]);{actorCountNum..actorCountNum+actorAppendNum-1} |> Seq.iter(fun a ->
                        // zeroNumArray.[a] <- int(parseMsg.[4])
                        zeroNum <- int(parseMsg.[4])
                        let s = parseMsg.[3] + System.Text.Encoding.ASCII.GetString( [|byte(0x20 + a)|])
                        actorArray.[a] <! TransitMsg(a, "go to work", s,zeroNum)
                        ()
                            );actorAppendNum <- actorCountNum+actorAppendNum
                      

            // | "find nothing" -> actori <- int(parseMsg.[1]); 
            //                     actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2]);
  
            | "bitcoin" ->  printfn "bitcoin: %s" parseMsg.[2];
                            actori <- int(parseMsg.[1]);
                            let strTemp = FindCoin.increaseString(parseMsg.[2])
                            actorArray.[actori] <! TransitMsg(actori, "go to work", strTemp, zeroNum);
                            coin_count <- coin_count+1
            
            | "client bitcoin" -> printfn "client bitcoin: %s" parseMsg.[2];
                                  for i in 0..client_count-1 do
                                    if clientAddArray.[i] = parseMsg.[1] then
                                        let tempStr = FindCoin.increaseString(parseMsg.[2])
                                        let cMsg = sprintf "go to work; ;%s;%d"  tempStr zeroNum
                                        clientRefs.[i] <! cMsg
                                  coin_count <- coin_count+1

            | _ -> printfn "manager doesn't understand"

            // if coin_count<10 then   
            //     return! loop()
            // else
            //     printfn "---return---"
            return! loop()
        }
    loop()

let serverRef = spawn system "server" myMonitor
printfn "server initial"

serverRef <! "start;null;5;xiongruoyang;6";;
System.Console.ReadLine() |> ignore
// #time "on"
