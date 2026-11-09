using System.Text;

namespace ManeDistributeurTickets;

class Program
{
    private static readonly string CheminFichier = Path.Combine(Path.GetTempPath(), "fnuméro.txt");
    private static readonly char[] Types = { 'V', 'R', 'I' };
    private const string ModeleMessage = "Votre numéro est {0}, il y a {1} personne(s) qui attendent avant vous.";

    static void Main(string[] args)
    {
        List<Client> clients = new List<Client>();
        StringBuilder message = new StringBuilder();

        try
        {
            InitialiserFichier();
            Console.WriteLine("=== Distributeur de tickets - Banque ===");
            Console.WriteLine($"Compteurs remis à zéro dans : {CheminFichier}");
            Console.WriteLine();

            bool continuer = true;

            while (continuer)
            {
                char? type = LireType();
                if (type is null)
                {
                    break;
                }

                string? numeroCompte = LireTexte("Numéro de compte : ", EstNumeroCompteValide,
                    "Le numéro de compte doit contenir uniquement des lettres et des chiffres.");
                if (numeroCompte is null)
                {
                    break;
                }

                string? nom = LireTexte("Nom : ", EstNomValide,
                    "Le nom doit contenir uniquement des lettres, espaces, tirets ou apostrophes.");
                if (nom is null)
                {
                    break;
                }

                string? prenom = LireTexte("Prénom : ", EstNomValide,
                    "Le prénom doit contenir uniquement des lettres, espaces, tirets ou apostrophes.");
                if (prenom is null)
                {
                    break;
                }

                int rang = AttribuerNumero(type.Value);
                string ticket = $"{type.Value}-{rang}";
                clients.Add(new Client(ticket, NomOperation(type.Value), numeroCompte, nom.ToUpper(), prenom));

                message.Clear();
                message.AppendFormat(ModeleMessage, ticket, rang - 1);
                Console.WriteLine();
                Console.WriteLine(message.ToString());
                Console.WriteLine();

                continuer = DemanderAutreClient();
            }
        }
        catch (IOException e)
        {
            Console.WriteLine($"{e.GetType().FullName} : {e.Message}");
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"{e.GetType().FullName} : {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().FullName} : {e.Message}");
        }
        finally
        {
            AfficherListeClients(clients);
        }
    }

    static void InitialiserFichier()
    {
        using StreamWriter scribe = new StreamWriter(CheminFichier, false);

        foreach (char type in Types)
        {
            scribe.WriteLine($"{type};0");
        }
    }

    static int AttribuerNumero(char type)
    {
        Dictionary<char, int> compteurs = LireCompteurs();
        compteurs[type]++;
        EcrireCompteurs(compteurs);
        return compteurs[type];
    }

    static Dictionary<char, int> LireCompteurs()
    {
        Dictionary<char, int> compteurs = new Dictionary<char, int>();

        foreach (char type in Types)
        {
            compteurs[type] = 0;
        }

        if (!File.Exists(CheminFichier))
        {
            return compteurs;
        }

        using StreamReader lecteur = new StreamReader(CheminFichier);
        string? ligne;

        while ((ligne = lecteur.ReadLine()) is not null)
        {
            string[] parties = ligne.Split(';');

            if (parties.Length == 2
                && parties[0].Length == 1
                && compteurs.ContainsKey(parties[0][0])
                && int.TryParse(parties[1], out int valeur)
                && valeur >= 0)
            {
                compteurs[parties[0][0]] = valeur;
            }
        }

        return compteurs;
    }

    static void EcrireCompteurs(Dictionary<char, int> compteurs)
    {
        using StreamWriter scribe = new StreamWriter(CheminFichier, false);

        foreach (char type in Types)
        {
            scribe.WriteLine($"{type};{compteurs[type]}");
        }
    }

    static char? LireType()
    {
        while (true)
        {
            Console.WriteLine("Type d'opération : V - Versement, R - Retrait, I - Informations (Q pour terminer)");
            Console.Write("Votre choix : ");
            string? saisie = Console.ReadLine();

            if (saisie is null)
            {
                return null;
            }

            string choix = saisie.Trim().ToUpper();

            if (choix == "Q")
            {
                return null;
            }

            if (choix.Length == 1 && Types.Contains(choix[0]))
            {
                return choix[0];
            }

            Console.WriteLine("Choix invalide, tapez V, R, I ou Q.");
        }
    }

    static string? LireTexte(string message, Func<string, bool> estValide, string messageErreur)
    {
        while (true)
        {
            Console.Write(message);
            string? saisie = Console.ReadLine();

            if (saisie is null)
            {
                return null;
            }

            string texte = string.Join(" ", saisie.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (texte.Length == 0)
            {
                Console.WriteLine("Ce champ est obligatoire.");
            }
            else if (!estValide(texte))
            {
                Console.WriteLine(messageErreur);
            }
            else
            {
                return texte;
            }
        }
    }

    static bool EstNumeroCompteValide(string texte)
    {
        return texte.All(char.IsLetterOrDigit);
    }

    static bool EstNomValide(string texte)
    {
        return texte.Any(char.IsLetter)
            && texte.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
    }

    static bool DemanderAutreClient()
    {
        while (true)
        {
            Console.Write("Prendre un autre numéro ? (O/N) : ");
            string? saisie = Console.ReadLine();

            if (saisie is null)
            {
                return false;
            }

            string reponse = saisie.Trim().ToUpper();

            if (reponse == "O")
            {
                Console.WriteLine();
                return true;
            }

            if (reponse == "N")
            {
                return false;
            }

            Console.WriteLine("Répondez par O ou N.");
        }
    }

    static string NomOperation(char type)
    {
        return type switch
        {
            'V' => "Versement",
            'R' => "Retrait",
            _ => "Informations"
        };
    }

    static void AfficherListeClients(List<Client> clients)
    {
        Console.WriteLine();
        Console.WriteLine("=== Liste des clients ===");

        if (clients.Count == 0)
        {
            Console.WriteLine("Aucun ticket attribué.");
            return;
        }

        Console.WriteLine($"{"Ticket",-8}{"Opération",-14}{"Compte",-15}{"Nom",-15}Prénom");

        foreach (Client client in clients)
        {
            Console.WriteLine($"{client.Ticket,-8}{client.Operation,-14}{client.NumeroCompte,-15}{client.Nom,-15}{client.Prenom}");
        }

        Console.WriteLine($"Total : {clients.Count} client(s).");
    }
}

record Client(string Ticket, string Operation, string NumeroCompte, string Nom, string Prenom);