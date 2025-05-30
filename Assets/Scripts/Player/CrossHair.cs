using UnityEngine;

public class CrossHair : MonoBehaviour
{
    [Header("CrossHair")] public Animator anim;

    public void CrossHairIn(bool flag)
    {
        if (flag)
        {
            anim.SetBool("isIn", true);
        }
        else
        {
            anim.SetBool("isIn", false);
        }
    }
}
