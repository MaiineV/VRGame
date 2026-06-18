using Services;
using Services.Database;
using Services.Progression;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    /// <summary>
    /// Diegetic menu board. Lists the drinks the player has unlocked in the day shop (hides locked
    /// ones) and refreshes whenever the unlock set changes.
    /// </summary>
    public sealed class RecipeMenuBoard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _recipesLabel;

        [Header("Format")]
        [SerializeField] private string _title = "MENU";
        [SerializeField] private string _separator = "\n--------------------\n";

        private IProgressionService _progression;
        private bool _bound;

        // OnEnable handles re-enables; Start retries in case OnEnable ran before the services were
        // registered (scene init-order race), so the unlock filter is applied before the first frame.
        void OnEnable() => Bind();
        void Start() => Bind();

        void OnDisable()
        {
            if (_bound && _progression != null) _progression.UnlocksChanged -= Refresh;
            _bound = false;
            _progression = null;
        }

        private void Bind()
        {
            if (!_bound && ServiceLocator.TryGet<IProgressionService>(out _progression))
            {
                _progression.UnlocksChanged += Refresh;
                _bound = true;
            }
            Refresh();
        }

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
                // Only advertise drinks the player has unlocked.
                if (_progression != null && !_progression.IsRecipeUnlocked(r.Id)) continue;
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

            _recipesLabel.text = sb.Length > 0 ? sb.ToString() : "(sin tragos desbloqueados)";
        }
    }
}
