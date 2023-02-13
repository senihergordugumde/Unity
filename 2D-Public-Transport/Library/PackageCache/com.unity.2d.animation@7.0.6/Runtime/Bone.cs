namespace UnityEngine.U2D.Animation
{
    // Component to store id of a SpriteBone so that it can be bind back when needed
    [AddComponentMenu("")]
    internal class Bone : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        string m_Guid;
        
        public string guid
        {
            get => m_Guid;
            set => m_Guid = value;
        }
    }
}