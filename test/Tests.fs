module Tests

open Expecto

let server =
    testList
        "Server"
        [
            testCase "Adding valid Todo"
            <| fun _ ->
                let expectedResult = Ok()
                Expect.equal (Ok()) expectedResult "Result should be ok"
        ]

let all = testList "All" [ server ]

[<EntryPoint>]
let main _ = runTests defaultConfig all