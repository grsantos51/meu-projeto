using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Catalogo da loja + itens que o jogador ja comprou (salvo em PlayerPrefs).
// Pagamentos sao SIMULADOS: nenhuma cobranca real e feita.
public enum CategoriaItem { Roupa, Carros, Armas, Dinheiro }

public class ItemLoja
{
    public string id, nome, descricao;
    public CategoriaItem categoria;
    public float preco;          // em R$ (simulado)
    public string slot;          // itens do mesmo slot: so um equipado por vez
    public int moedas;           // pacotes de dinheiro do jogo
    public Color cor = Color.white;
    public bool Consumivel => moedas > 0;
}

public static class Inventario
{
    public static event Action Mudou;

    public static readonly List<ItemLoja> Catalogo = new List<ItemLoja>
    {
        // ROUPA DA ARISSA
        new ItemLoja { id = "oculos",   nome = "Óculos Escuros",   categoria = CategoriaItem.Roupa, preco = 4.90f, slot = "rosto",   cor = new Color(0.1f,0.1f,0.12f), descricao = "Estilo de quem nunca é reconhecida." },
        new ItemLoja { id = "bone",     nome = "Boné Neon",        categoria = CategoriaItem.Roupa, preco = 5.90f, slot = "cabeca",  cor = new Color(1f,0.2f,0.75f),  descricao = "Boné rosa que brilha à noite." },
        new ItemLoja { id = "chapeu",   nome = "Chapéu Panamá",    categoria = CategoriaItem.Roupa, preco = 6.90f, slot = "cabeca",  cor = new Color(0.93f,0.85f,0.65f), descricao = "Perfeito para a praia." },
        new ItemLoja { id = "corrente", nome = "Corrente de Ouro", categoria = CategoriaItem.Roupa, preco = 9.90f, slot = "pescoco", cor = new Color(1f,0.78f,0.2f),  descricao = "Ouro 18k (de mentirinha)." },
        new ItemLoja { id = "mochila",  nome = "Mochila",          categoria = CategoriaItem.Roupa, preco = 4.90f, slot = "costas",  cor = new Color(0.15f,0.55f,1f), descricao = "Cabe muito dinheiro aqui dentro." },

        // CARROS
        new ItemLoja { id = "pint_azul",    nome = "Pintura Azul",          categoria = CategoriaItem.Carros, preco = 3.90f, slot = "pintura", cor = new Color(0.15f,0.45f,1f),  descricao = "Pinta o carro que você dirigir." },
        new ItemLoja { id = "pint_roxa",    nome = "Pintura Roxa",          categoria = CategoriaItem.Carros, preco = 3.90f, slot = "pintura", cor = new Color(0.6f,0.2f,1f),   descricao = "Pinta o carro que você dirigir." },
        new ItemLoja { id = "pint_preta",   nome = "Pintura Preta Fosca",   categoria = CategoriaItem.Carros, preco = 4.90f, slot = "pintura", cor = new Color(0.12f,0.12f,0.13f), descricao = "Discreta para fugir da polícia." },
        new ItemLoja { id = "pint_dourada", nome = "Pintura Dourada",       categoria = CategoriaItem.Carros, preco = 7.90f, slot = "pintura", cor = new Color(1f,0.75f,0.15f), descricao = "Para quem gosta de aparecer." },
        new ItemLoja { id = "neon_baixo",   nome = "Neon Embaixo do Carro", categoria = CategoriaItem.Carros, preco = 6.90f, slot = "neon",    cor = new Color(0.1f,1f,1f),    descricao = "Luz neon ciano por baixo do carro." },
        new ItemLoja { id = "carro_vip",    nome = "Carro Exclusivo GRS",   categoria = CategoriaItem.Carros, preco = 19.90f, slot = null,     cor = new Color(1f,0.85f,0.1f), descricao = "Esportivo mais rápido, estacionado na loja." },

        // ARMAS E ITENS
        new ItemLoja { id = "taco",        nome = "Taco de Beisebol", categoria = CategoriaItem.Armas, preco = 7.90f, slot = "mao",   cor = new Color(0.7f,0.5f,0.3f), descricao = "Soco com mais alcance." },
        new ItemLoja { id = "soco_ingles", nome = "Soco Inglês",      categoria = CategoriaItem.Armas, preco = 4.90f, slot = "mao",   cor = new Color(0.85f,0.85f,0.9f), descricao = "Soca duas vezes mais rápido." },
        new ItemLoja { id = "colete",      nome = "Colete",           categoria = CategoriaItem.Armas, preco = 9.90f, slot = "corpo", cor = new Color(0.2f,0.25f,0.2f), descricao = "A polícia demora o dobro para te prender." },

        // DINHEIRO DO JOGO
        new ItemLoja { id = "din_1k",   nome = "$ 1.000",   categoria = CategoriaItem.Dinheiro, preco = 1.90f,  moedas = 1000,   cor = new Color(0.45f,1f,0.45f), descricao = "Um troco para começar." },
        new ItemLoja { id = "din_5k",   nome = "$ 5.000",   categoria = CategoriaItem.Dinheiro, preco = 4.90f,  moedas = 5000,   cor = new Color(0.45f,1f,0.45f), descricao = "Igual a um assalto ao banco." },
        new ItemLoja { id = "din_20k",  nome = "$ 20.000",  categoria = CategoriaItem.Dinheiro, preco = 9.90f,  moedas = 20000,  cor = new Color(0.45f,1f,0.45f), descricao = "Mais vendido!" },
        new ItemLoja { id = "din_100k", nome = "$ 100.000", categoria = CategoriaItem.Dinheiro, preco = 29.90f, moedas = 100000, cor = new Color(0.45f,1f,0.45f), descricao = "Vida de milionária." },
    };

    const string ChaveItens = "GRS1_Itens";
    static HashSet<string> comprados;

    static HashSet<string> Comprados
    {
        get
        {
            if (comprados == null)
                comprados = new HashSet<string>(PlayerPrefs.GetString(ChaveItens, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            return comprados;
        }
    }

    public static ItemLoja Item(string id) => Catalogo.FirstOrDefault(i => i.id == id);
    public static bool Tem(string id) => Comprados.Contains(id);

    // ----- provador: o que a Arissa esta provando agora (slot -> id; "" = sem nada) -----
    public static readonly Dictionary<string, string> Prova = new Dictionary<string, string>();
    public static bool Visivel(ItemLoja it) => it.slot != null && (Prova.TryGetValue(it.slot, out var p) ? p == it.id : EstaEquipado(it));
    public static bool Provando(ItemLoja it) => it.slot != null && Prova.TryGetValue(it.slot, out var p) && p == it.id;
    public static void Provar(ItemLoja it)
    {
        if (it.slot == null) return;
        if (Provando(it)) Prova.Remove(it.slot); else Prova[it.slot] = it.id;
        Mudou?.Invoke();
    }
    public static void LimparProva() { if (Prova.Count == 0) return; Prova.Clear(); Mudou?.Invoke(); }
    // itens provados que ainda nao sao seus
    public static List<ItemLoja> Carrinho => Prova.Values.Where(id => !string.IsNullOrEmpty(id) && !Tem(id)).Select(Item).Where(i => i != null).ToList();

    public static string Equipado(string slot) => string.IsNullOrEmpty(slot) ? null : PlayerPrefs.GetString("GRS1_Equip_" + slot, "");
    public static bool EstaEquipado(ItemLoja it) => it.slot != null && Equipado(it.slot) == it.id;
    public static ItemLoja ItemEquipado(string slot) { var id = Equipado(slot); return string.IsNullOrEmpty(id) ? null : Item(id); }

    // Chamado depois do pagamento aprovado
    public static void Entregar(ItemLoja it)
    {
        if (it.Consumivel)
        {
            if (Dinheiro.Instancia) Dinheiro.Instancia.Adicionar(it.moedas);
            else PlayerPrefs.SetInt("GRS1_Dinheiro", PlayerPrefs.GetInt("GRS1_Dinheiro", 0) + it.moedas);
        }
        else
        {
            Comprados.Add(it.id);
            PlayerPrefs.SetString(ChaveItens, string.Join(",", Comprados));
            if (it.slot != null) { PlayerPrefs.SetString("GRS1_Equip_" + it.slot, it.id); Prova.Remove(it.slot); } // ja sai equipado
        }
        PlayerPrefs.Save();
        Mudou?.Invoke();
    }

    public static void AlternarEquipar(ItemLoja it)
    {
        if (it.slot == null || !Tem(it.id)) return;
        PlayerPrefs.SetString("GRS1_Equip_" + it.slot, EstaEquipado(it) ? "" : it.id);
        PlayerPrefs.Save();
        Mudou?.Invoke();
    }

    public static void Zerar()
    {
        PlayerPrefs.DeleteKey(ChaveItens);
        foreach (var s in Catalogo.Where(i => i.slot != null).Select(i => i.slot).Distinct()) PlayerPrefs.DeleteKey("GRS1_Equip_" + s);
        comprados = null;
        PlayerPrefs.Save();
        Mudou?.Invoke();
    }

    public static string Reais(float v) => "R$ " + v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
}
