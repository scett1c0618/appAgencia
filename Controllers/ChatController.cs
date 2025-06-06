using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using app1.Servicios;

namespace app1.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatbotService _chatbotService;
        public ChatController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensaje(string mensaje)
        {
            var respuesta = await _chatbotService.ObtenerRespuestaAsync(mensaje);
            ViewBag.MensajeUsuario = mensaje;
            ViewBag.RespuestaBot = respuesta;
            return View("Index");
        }
    }
}
