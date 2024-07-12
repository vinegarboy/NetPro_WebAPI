using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
class Program{
    public record UserRequest(string UserName);
    public record UserSearchRequest(string id);
    public record UserBuyRequest(string id, int coinA_Value, int coinB_Value);
    static void Main(string[] args){
        //100A = 1B
        float coin_AtoB = 100;

        List<UserData> users = new List<UserData>();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()){
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        Task changeRate = Task.Run(async () => {
            while(true){
                //1秒枚に変動
                await Task.Delay(1000);
                coin_AtoB += (float)((2*new Random().NextDouble()/10)-0.1);
            }
        });


        app.MapGet("/AtoBRate", () => coin_AtoB).WithName("GetAtoB_Rate").WithOpenApi();

        app.MapGet("/BtoARate", () => 1/coin_AtoB).WithName("GetBtoA_Rate").WithOpenApi();

        app.MapGet("/Ranking",()=>{
            var user_list = GetUserTopScores(users);
            return JsonSerializer.Serialize(user_list).ToString();
        }).WithName("GetRanking").WithOpenApi();
        
        app.MapPost("/Register", (UserRequest request) =>{
            var usr = new UserData(request.UserName);
            users.Add(usr);
            return Results.Ok(new {Code=200,ID = usr.id,Message = $"Register Success.\nHello, {request.UserName}" });
        }).WithName("UserRegister").WithOpenApi();

        app.MapPost("/GetUserData",(UserSearchRequest request)=>{
            var user = users.Find(user => user.id == request.id);
            if(user == null){
                return Results.NotFound(new {Code=404, Message = "User Not Found."});
            }
            return Results.Ok(new {Code=200, Message = JsonSerializer.Serialize(user).ToString()});
        }).WithName("GetUserData").WithOpenApi();

        app.MapPost("/BuyOrder",(UserBuyRequest request)=>{
            var user = users.Find(user => user.id == request.id);
            if(user == null){
                return Results.NotFound(new {Code=404, Message = "User Not Found."});
            }
            if(request.coinA_Value*(1/coin_AtoB) > user.coinB || request.coinB_Value*coin_AtoB > user.coinA){
                return Results.BadRequest(new {Code=400, Message = "Not Enough Coin."});
            }
            user.coinA -= request.coinB_Value*coin_AtoB;
            user.coinB += request.coinB_Value;
            user.coinB -= request.coinA_Value*(1/coin_AtoB);
            user.coinA += request.coinB_Value;
            return Results.Ok(new {Code=200, Message = "Buy Order Success."});
        }).WithName("BuyOrder").WithOpenApi();
        app.Run();
    }

    public static List<UserData> GetUserTopScores(List<UserData> users){
        // 配列の長さが6以下の場合は並び替えだけで返す
        if (users.Count <= 6){
            return users.OrderByDescending(user => user.Score()).ToList();
        }

        // 上位6名を返す
        return users.OrderByDescending(user => user.Score()).Take(6).ToList();
    }
}


class UserData{
    public static int Setid = 0;
    public string Name { get; set; }
    public string id{get;set;}
    public float coinA { get; set; }
    public float coinB { get; set; }

    public UserData(string Name){
        this.Name = Name;
        this.id = Setid.ToString();
        Setid++;
        this.coinA = 1000;
        this.coinB = 10;
    }

    public float Score(){
        return coinA/1000 + coinB/10;
    }
}