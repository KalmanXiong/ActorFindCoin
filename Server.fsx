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
    let rec loop() =
        actor {
            let! msg = mailbox.Receive()
            // let sender = mailbox.Sender()

            let parseMsg = msg.Split ';'
            // "content;id/addr;paramter"
            match parseMsg.[0] with
            | "register" -> printfn "welcome: %s" parseMsg.[1];
                            
            | "start" -> actorAppendNum <- int(parseMsg.[2]);{actorCountNum..actorCountNum+actorAppendNum-1} |> Seq.iter(fun a ->
                        zeroNumArray.[a] <- int(parseMsg.[4])
                        let s = parseMsg.[3] + System.Text.Encoding.ASCII.GetString( [|byte(0x20 + a)|])
                        actorArray.[a] <! TransitMsg(a, "go to work", s,zeroNumArray.[a])
                        ()
                            );actorAppendNum <- actorCountNum+actorAppendNum

            | "bitcoin" ->  printfn "bitcoin: %s" parseMsg.[2];
                            actori <- int(parseMsg.[1]);
                            zeroNumArray.[actori] <- zeroNumArray.[actori] + 1;
                            actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2],zeroNumArray.[actori]);

            | "find nothing" -> actori <- int(parseMsg.[1]); 
                                actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2],zeroNumArray.[actori]);
                                ///asdadadsascdwefe

            | "client bitcoin" -> printfn "bitcoin: %s" parseMsg.[2]
                                  
            | _ -> printfn "manager doesn't understand"             
            return! loop()
        } 
    loop()

let serverRef = spawn system "server" myMonitor
printfn "server initial"
serverRef <! "start; ;4;xiongruoyang;1";;
System.Console.ReadLine() |> ignore
