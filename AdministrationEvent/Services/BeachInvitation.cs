using AdministrationEvent.Models;

namespace AdministrationEvent.Services
{
    public static class BeachInvitation
    {
        public static InvitationConfiguration Create() => new InvitationConfiguration {
            Title = "Despedida · una escapada entre amigas",
            Slug = "despedida-sorpresa",
            EventType = "Despedida de soltera",
            Hosts = "Chea\nNos vamosss!",
            Message = "Última escapada de soltera. El resto es sorpresa.",
            Date = new System.DateTime(2027, 2, 24),
            HasConfirmedTime = false,
            CoverImageUrl = "/images/despedida/portada-bikini-velo-v4.png",
            Theme = new InvitationTheme { Layout = "beach", Background = "#fff9ed", Text = "#435b59", Accent = "#617b4f", Secondary = "#617b4f", Highlight = "#f3dda0" },
            Sections = new System.Collections.Generic.List<InvitationSection> {
                new InvitationSection { Title = "Reservate estos días", ImageUrl = "/images/despedida/calendario-playita-v2.png", Content = "24 de febrero de 2027\nal 3 de marzo de 2027\n\nSalida · 7:20 am\nEstar en el aeropuerto 2 horas antes" },
                new InvitationSection { Title = "Dress code", ImageUrl = "/images/despedida/bikinis-cuerda-v3.png", Content = "Imprescindible bikini y ropita de verano\n\nDe a poquito te vamos a revelar la paleta de colores de cada día, para que elijas tu color y armes tus looks." },
                new InvitationSection { Title = "El plan", ImageUrl = "/images/despedida/plan-cinco-sillitas-dibujo-v2.png", Content = "Sol, mar y arena y mucho alcohol." },
                new InvitationSection { Title = "El destino?", ImageUrl = "/images/despedida/avioncito-detalle-v1.png", Content = "Por ahora es nuestro secreto. Vos solo ocupate de hacer lugar en la valija." }
            }
        };
    }
}
