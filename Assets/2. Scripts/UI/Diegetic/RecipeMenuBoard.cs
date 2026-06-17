using Data.SO;
using Services;
using Services.Database;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    public sealed class RecipeMenuBoard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _recipesLabel;

        [Header("Format")]
        [SerializeField] private string _title = "MENU";
        [SerializeField] private string _separator = "\n--------------------\n";

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (_titleLabel != null) _titleLabel.text = _title;
            if (_recipesLabel == null) return;
            if (!ServiceLocator.TryGet<IDatabaseService>(out var db)) return;

            var recipes = db.AllRecipes;
            if (recipes == null || recipes.Count == 0)
            {
                _recipesLabel.text = "(sin recetas)";
                return;
            }

            var sb = new System.Text.StringBuilder(512);
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                if (r == null) continue;
                if (sb.Length > 0) sb.Append(_separator);

                sb.Append("<b>").Append(r.DisplayName).Append("</b>");
                sb.Append("  $").Append(r.BasePrice);
                sb.Append('\n');

                if (r.Steps != null)
                {
                    for (int s = 0; s < r.Steps.Length; s++)
                    {
                        var step = r.Steps[s];
                        var ing = db.GetIngredient(step.id);
                        string name = ing != null ? ing.DisplayName : step.id.ToString();
                        int ml = Mathf.RoundToInt(step.targetMl);
                        sb.Append("  - ").Append(name).Append(" (").Append(ml).Append(" ml)\n");
                    }
                }
            }

            _recipesLabel.text = sb.ToString();
        }
    }
}
