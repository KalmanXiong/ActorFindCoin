# DOSP Project 1

# Group Member
Member 1:
        Name: Xiao Li
        UFID: 3475-9600
        Email: xiao.li@ufl.edu

Member 2: 
        Name: Ruoyang Xiong
        UFID: 5311-2826
        Email: xiongruoyang@ufl.edu

# Components
project1.zip
|- Server.fsx
|- Client.fsx

# The result of running your program for input 4
Some samples:

xiongruoyang# @M1       00008B4B52C983467F76EBC700052A83D3DB0893421221C7ACC637A8BE294E07
xiongruoyang  HVy       0000AA1CAA6A334C4801C9AFD148FF75C5136E466A78F0457D6545AF50BF53F1
xiongruoyang  I7{       00002D6B235BDCD278D668DCAAEF56353213BD01ED302617AB81CA20236D0588
xiongruoyang  JLD       00000C10FE92B2F3601111987F8F533936DF9F4D8B60699A3B75AC0A1035D821
xiongruoyang! 0t$       000062D0F3627BD4A761C5E37C8E3DA12C6D3E2D068102445862DF393BE603FD
xiongruoyang" k^#       00002CCF8BFBAA49A7043FEBCDAB21B3F6466DE062CE9E009362136FDE0FF8A5
xiongruoyang! 3]y       000041D18AE1637EC934D74EB5558BF2D0D3A2023D1AB8F9C823777209D557BA
xiongruoyang# MH3       0000144F875BFD7001B261CB839ACCC8BBEA8EE9EE9373E55E42DB931F5DBE0A
xiongruoyang# NQ0       0000343F73D10D29A6B95895C5B9787DF2982DB559711BC1C78EABD61FD63123
xiongruoyang" tHg       0000810AD598FFA6AC99396C5FF5D832143533E967571A1AC419A41D47FF8F80
xiongruoyang! :`2       0000F7DF6CA3EAD4D3278007B34BC9B4A3405DE34F96300A12BB8A1E36BBA1F3

# The running time for the above

The Server is tested on Window system with 4 cpu cores. In this experiment, we create a monitor and 3 workers. we print the real time and cpu time as following:

CPU time = 49640 ms    Real time = 19201 ms   CPU time/Real time = 2.585315
CPU time = 99046 ms    Real time = 40997 ms   CPU time/Real time = 2.415954
CPU time = 172671 ms   Real time = 71498 ms   CPU time/Real time = 2.415059
CPU time = 217031 ms   Real time = 90458 ms   CPU time/Real time = 2.399249
CPU time = 298640 ms   Real time =125232 ms   CPU time/Real time = 2.384699
CPU time = 354640 ms   Real time =146412 ms   CPU time/Real time = 2.422210
CPU time = 391281 ms   Real time =162018 ms   CPU time/Real time = 2.415048

# The coin with the most 0s you managed to find.

The coin with the most 0s we find is 
BitCoin:
Hash Code:


# The largest number of working machines you were able to run your code with

We test our code on two machines currently. However, our code can be adapted more machines if necessary. 

Assuming that we only deploy one actor on each machine, the current largest number of working machines is 256. This depends on how we split a task into more subtask. In our code, if necessary, we can easily change the number to larger one. In theory, it can accommodate any number of machines.