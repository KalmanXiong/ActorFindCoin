#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "System"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"
#load "FindCoin.fsx"

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
    | TransitMsg of int * string * string

let actor_num = 10
let system = System.create "Server" configuration

let myActor (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! TransitMsg(n, content, param) = mailbox.Receive()
        let sender = mailbox.Sender()
        match content with
        | "go to work" -> printfn "local actor %d start to work" n ; FindCoin.findCoin(param, 7)
        | _ -> printfn "actor don't understand"
        let returnMsg = sprintf "bitcoin;%d;%s;"  n "bincoin sequence"
        sender <! returnMsg
        return! loop()
    }
    loop()

let myMonitor (mailbox: Actor<string>) =
    let mutable i = 0
    let mutable n = 0
    let mutable actori = 0

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
                            
            | "start" -> n <- int(parseMsg.[2]);{i..i+n-1} |> Seq.iter(fun a ->
                        actorArray.[a] <! TransitMsg(a, "go to work", "init string")
                        ()
                            ); i <- n+i

            | "bitcoin" ->  printfn "bitcoin: %s" parseMsg.[2];
                            actori <- int(parseMsg.[1]); 
                            actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2]);

            | "find nothing" -> actori <- int(parseMsg.[1]); 
                                actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2]);

            | "client bitcoin" -> printfn "bitcoin: %s" parseMsg.[2]
                                  
            | _ -> printfn "manager doesn't understand"             
            return! loop()
        } 
    loop()

let serverRef = spawn system "server" myMonitor
printfn "server initial"
serverRef <! "start; ;5";;
System.Console.ReadLine() |> ignore
