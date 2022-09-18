
[System.Serializable]
public abstract class State : BaseNode
{
    public abstract void OnEnter();
    public abstract void OnExit();

}