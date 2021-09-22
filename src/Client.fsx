#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"
#load "findCoin.fsx"

open System
open Akka.FSharp
open Akka.Remote
open Akka.Configuration
open FindCoin

let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                
            }
            remote {
                helios.tcp {
                    port = 2552
                    hostname = localhost
                }
            }
        }")
 
let clientSystem = System.create "Client" configuration
let serverIP = "akka.tcp://Server@127.0.0.1:9003/user/server"
let serverRef = select (serverIP) clientSystem

let actor_num = 10

type MessageSystem =
    | TransitMsg of int * string * string

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

    let actorArray = Array.create actor_num (spawn clientSystem "myActor" myActor)
    {0..actor_num-1} |> Seq.iter (fun a ->
        actorArray.[a] <- spawn clientSystem (string a) myActor
        ()
    )
    // let serverRef = select (parseMsg.[1]) myMonitor

    let rec loop() =
        actor {
            let! msg = mailbox.Receive()
            let sender = mailbox.Sender()
            let parseMsg = msg.Split ';'
            // "content;id/addr;paramter"
            match parseMsg.[0] with
                            
            | "start" -> n <- int(parseMsg.[2]);{i..i+n-1} |> Seq.iter(fun a ->
                        actorArray.[a] <! TransitMsg(a, "go to work", "init string")
                        ()
                            ); i <- n+i

            | "bitcoin" ->  printfn "bitcoin: %s" parseMsg.[2];
                            actori <- int(parseMsg.[1]); 
                            actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2]);
                            let remoteMsg = sprintf "remote bitcoin; ;parseMsg.[2]"
                            serverRef <! remoteMsg

            | "find nothing" -> actori <- int(parseMsg.[1]); 
                                actorArray.[actori] <! TransitMsg(actori, "go to work", parseMsg.[2]);
                                  
            | _ -> printfn "manager doesn't understand"             
            return! loop()
        } 
    loop()

let clientRef = spawn clientSystem "client" myMonitor
let myIP = "akka.tcp://Client@127.0.0.1:2552/user/client"
let regisMsg = sprintf "register;%s; " myIP
serverRef <! regisMsg;;
Console.ReadLine()
