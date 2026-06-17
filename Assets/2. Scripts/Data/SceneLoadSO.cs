using UnityEngine;

namespace Data
{
    /// <summary>
    /// Configuración de tiempos y flags para el flujo de carga de escenas.
    /// Colocá un asset en Resources/SceneLoadSO para sobreescribir valores por juego/escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Scene Load Config", fileName = "SceneLoadSO")]
    public class SceneLoadSO : ScriptableObject
    {
        [Tooltip("Segundos mínimos que la pantalla de Loading debe permanecer visible.")]
        [Min(0f)] public float minimumLoadingSeconds = 1f;

        [Tooltip("En player, llamar a Resources.UnloadUnusedAssets() + GC al terminar el loading.")]
        public bool unloadUnusedAssetsInPlayer = true;
    }
}
