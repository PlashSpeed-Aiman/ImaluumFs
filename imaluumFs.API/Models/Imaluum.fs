module WebApplication3.Models

type Login = {
    Username : string
    Password : string
}

[<Literal>]
let iMaluumBase = "https://imaluum.iium.edu.my/"
[<Literal>]
let iMaluumLoginUrl = "https://cas.iium.edu.my:8448/cas/login?service=https%3a%2f%2fimaluum.iium.edu.my%2fhome"
[<Literal>]
let iMaluumPostUrl ="https://cas.iium.edu.my:8448/cas/login?service=https%3a%2f%2fimaluum.iium.edu.my%2fhome?service=https%3a%2f%2fimaluum.iium.edu.my%2fhome"