#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "System"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"
#load "findCoin.fsx"

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

type MessageSystem =
    | TransitMsg of int * string * string * int

let actor_num = 10
let system = System.create "Server" configuration


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
        return! loop()
    }
    loop()

// let clientRegister addr, addrArray refArray= 

let myMonitor (mailbox: Actor<string>) =
    let mutable actorCountNum = 0
    let mutable actorAppendNum = 0
    let mutable actori = 0
    let mutable zeroNumArray = Array.create actor_num 0

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
                            
                            let cMsg = sprintf "go to work; ;%s" parseMsg.[2]//新任务
                            clientRefs.[client_count-1] <! cMsg
                            printfn "Count %d, Welcome: %s" client_count parseMsg.[1];

            | "start" -> actorAppendNum <- int(parseMsg.[2]);{actorCountNum..actorCountNum+actorAppendNum-1} |> Seq.iter(fun a ->
                        zeroNumArray.[a] <- int(parseMsg.[4])
                        let s = parseMsg.[3] + System.Text.Encoding.ASCII.GetString( [|byte(0x20 + a)|])
                        actorArray.[a] <! TransitMsg(a, "go to work", s,zeroNumArray.[a])
                        ()
                            );actorAppendNum <- actorCountNum+actorAppendNum
                      

            // | "find nothing" -> actori <- int(parseMsg.[1]); 
            //                     actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2]); //自增后的任务
            | "bitcoin" ->  printfn "bitcoin: %s" parseMsg.[2];
                            actori <- int(parseMsg.[1]);
                            // zeroNumArray.[actori] <- zeroNumArray.[actori] + 1;
                            actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2], zeroNumArray.[actori]);
            
            | "client bitcoin" -> printfn "client bitcoin: %s" parseMsg.[2];
                                  let mutable temp_str = ""
                                  for i in 0..client_count-1 do
                                    if clientAddArray.[i] = parseMsg.[1] then
                                        temp_str <- FindCoin.increaseString(parseMsg.[2])
                                        let cMsg = sprintf "go to work; ;%s" temp_str  
                                        clientRefs.[i] <! cMsg

            | _ -> printfn "manager doesn't understand"             
            return! loop()
        }
    loop()

let serverRef = spawn system "server" myMonitor
printfn "server initial"

serverRef <! "start; ;4;xiongruoyang;1";; 
System.Console.ReadLine() |> ignore
