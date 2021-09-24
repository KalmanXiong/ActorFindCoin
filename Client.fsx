#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"
#load "FindCoin.fsx"

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
                    port = 1000
                    hostname = 10.136.17.122
                }
            }
        }")
 
let clientSystem = System.create "Client" configuration
let serverIP = "akka.tcp://Server@10.136.106.52:9001/user/server"
let serverRef = select (serverIP) clientSystem

type MessageSystem =
    | TransitMsg of int * string * string

let parseBitcoin(s:string) =    let mutable str = s
                                for i in 1..3 do
                                    str <- str.[str.IndexOf(";")+1..]
                                str

let myActor ip (mailbox: Actor<string>) =
    let regisMsg = sprintf "register;%s" ip
    serverRef <! regisMsg
    let mutable str = ""
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        printfn "receive from server: %s" msg
        let parseMsg = msg.Split ';'

        match parseMsg.[0] with
        | "go to work" -> str <- FindCoin.findCoin(parseBitcoin(msg), int(parseMsg.[1]), int(parseMsg.[2]))
                        //   printfn "local actor new work";
        | _ -> printfn "actor don't understand"
        let returnMsg = sprintf "bitcoin client;%s;_;%s"  ip str
        serverRef <! returnMsg
        return! loop()
    }
    loop()


for i in 1..3 do 
    let myIP = sprintf "akka.tcp://Client@10.136.17.122:1000/user/client%d" i
    let actorName = sprintf "client%d" i
    let clientRef = spawn clientSystem actorName (myActor myIP)
    printfn "local %s starts" actorName

Console.ReadLine()
