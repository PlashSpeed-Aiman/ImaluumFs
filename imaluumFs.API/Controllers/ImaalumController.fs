namespace WebApplication3.Controllers

open System.Collections.Generic
open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open WebApplication3.Models
[<ApiController>]
[<Route("[controller]")>]
type ImaalumController (logger : ILogger<ImaalumController>, _httpClientFactory: IHttpClientFactory) =
    inherit ControllerBase()
    
    [<HttpGet("Login")>]
    member this.Login([<FromQuery>] year:string, [<FromQuery>] semester:string) =
        let x = async{
            let encodedData = System.Web.HttpUtility.UrlEncode($"{year},{semester}")
            let client = _httpClientFactory.CreateClient()
            let task = task{
                let! imaluumClient = this.SetupClientAsync client
                let! result = imaluumClient |> fun (x:HttpClient) -> x.GetAsync($"https://imaluum.iium.edu.my/MyAcademic/confirmation-slip?sessem={encodedData}")
                let! responseContent = result.Content.ReadAsStreamAsync()
                use ms = new System.IO.MemoryStream()
                do! responseContent.CopyToAsync(ms)
                let bytes = ms.ToArray()
                return bytes
            } 
            let! res = task |> Async.AwaitTask 
            return res
        }
        this.File( x |> Async.RunSynchronously,"text/html","result.html")
        
        

    
    member private this.SetupClientAsync(client : HttpClient) : Task<HttpClient> =
        task{
            let! response = client
                                |> this.SetHeaders
                                |> fun (x: HttpClient) -> x.GetAsync(iMaluumLoginUrl) 
            let! result =  this.SetCookies response client
                              |> fun (x:HttpClient) -> x.PostAsync(iMaluumPostUrl,this.SetFormValues("",""))
            return this.SetCookies result client
            }
    member private this.SetHeaders(client : HttpClient) =
        client.DefaultRequestHeaders.Add("Connection", "Keep-Alive")
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0")
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US")
        client
    
    member private this.SetCookies(responseObject : HttpResponseMessage) (client:HttpClient) =
        let clientCookie =
            match client.DefaultRequestHeaders.TryGetValues("Cookie") with
                | true, headers -> headers
                | false, _ -> Seq.empty
                
        let cookieHeaders =
            match responseObject.Headers.TryGetValues("Set-Cookie") with
            | true, headers -> headers
            | false, _ -> Seq.empty
        let cookies = cookieHeaders |> Seq.append (clientCookie)
        client.DefaultRequestHeaders.Add("Cookies",cookies)
        client
        
    
    member private this.SetFormValues(username,password) =
       
        let data : IEnumerable<KeyValuePair<string,string>> = 
            seq {
                yield KeyValuePair("username", username)
                yield KeyValuePair("password", password)
                yield KeyValuePair("execution", "e1s1")
                yield KeyValuePair("_eventId", "submit")
                yield KeyValuePair("geolocation", "")
            }
        let form = new FormUrlEncodedContent(data)
        form
       