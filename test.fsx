let parseBitcoin(s:string) =    let mutable str = s
                                for i in 1..3 do
                                    str <- str.[str.IndexOf(";")+1..]
                                str

let str = "asd;123;ert;345"
let result = parseBitcoin(str)
printfn "%s" result