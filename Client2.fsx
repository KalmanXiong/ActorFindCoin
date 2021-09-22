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
                    port = 2556
                    hostname = 127.0.0.1
                }
            }
        }")
 
let clientSystem = System.create "Client" configuration
let serverIP = "akka.tcp://Server@127.0.0.1:9003/user/server"
let serverRef = select (serverIP) clientSystem

let actor_num = 10

type MessageSystem =
    | TransitMsg of int * string * string

// let myActor (mailbox: Actor<_>) = 
//     let rec loop() = actor {
//         let! TransitMsg(n, content, param) = mailbox.Receive()
//         let sender = mailbox.Sender()
//         match content with
//         | "go to work" -> printfn "local actor %d start to work" n ; FindCoin.findCoin(param, 7)
//         | _ -> printfn "actor don't understand"
//         let returnMsg = sprintf "bitcoin;%d;%s;"  n "bincoin sequence"
//         sender <! returnMsg
//         return! loop()
//     }
//     loop()

let myActor ip (mailbox: Actor<string>) =
    let regisMsg = sprintf "register;%s; " ip
    serverRef <! regisMsg
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        printfn "receive from server: %s" msg
        let parseMsg = msg.Split ';'
        match parseMsg.[0] with
        | "go to work" -> printfn "local actor start to work"; FindCoin.findCoin(parseMsg.[2], 4)
        | _ -> printfn "actor don't understand"

        let returnMsg = sprintf "client bitcoin;%s;%s;"  ip "bincoin sequence"
        serverRef <! returnMsg
        return! loop()
    }
    loop()
for i in 1..10 do 
    let myIP = sprintf "akka.tcp://Client@127.0.0.1:2556/user/client%d" i
    let actorName = sprintf "client%d" i
    let clientRef = spawn clientSystem actorName (myActor myIP)
    printfn "local %s starts" actorName





Console.ReadLine()
