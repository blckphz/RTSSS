using System.Collections;

public interface IAttackAnimation
{
    void PlayAttackAnimation();

    IEnumerator WaitForAttackFinished();
}