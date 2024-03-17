using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ValidaUsuario;

public class Function
{

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest context)
    {

        using (MySqlConnection conexao = new MySqlConnection("Server=localhost;Port=3306;Database=TechChallenge;User=TechChallenge;Password=TechChallengeSoat4;"))
        {
            try
            {
                conexao.Open();
                JObject contextObject = JObject.Parse(context.Body);
                var cpf = contextObject["cpf"]?.ToString();

                var command = $"SELECT * FROM TechChallenge.Cliente WHERE cpf = {cpf}";
                MySqlCommand comando = new MySqlCommand(command, conexao);
                MySqlDataReader reader = comando.ExecuteReader();

                if (reader.Read())
                {
                    var token = GenerateToken(cpf);
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = token
                    };
                }
                else
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound
                    };
                }
            }
            catch (Exception ex)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = ex.ToString()
                };
            }
            finally { conexao.Close(); }
        }
    }

    private string GenerateToken(string cpf)
    {
        var claims = new[]
        {
            new Claim("cpf", cpf),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var privateKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("privateKeyTechChallenger Grupo 4SOAT"));

        var credentials = new SigningCredentials(privateKey, SecurityAlgorithms.HmacSha256);

        var expiration = DateTime.UtcNow.AddMinutes(60);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: "TechChallengerIssuer",
            audience: "TechChallengerAudience",
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
