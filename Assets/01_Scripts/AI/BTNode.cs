using System;

// 부모 노드에게 결과를 알리는 열거형
public enum BTNodeStatus
{
    Failure, Success, Running
}

public abstract class BTNode
{
    public abstract BTNodeStatus Evaluate();

    // 상위 노드가 다른 행동을 했을 때 호출해 상태를 정리
    public virtual void Abort()
    {

    }
}

// Composite Node : Selector
public class BTSelector : BTNode
{
    private readonly BTNode[] children;

    // 이전 프레임에 Running 상태였던 자식을 기억
    private BTNode runningChild;

    public BTSelector(params BTNode[] children)
    {
        this.children = children;
    }
    
    // 첫 번째 자식부터 평가해 실행 가능한 첫 번째 자식의 결과를 반환
    public override BTNodeStatus Evaluate()
    {
        foreach (BTNode child in children)
        {
            BTNodeStatus childStatus = child.Evaluate();

            if (childStatus == BTNodeStatus.Failure)
            {
                // 실행 중이던 자식이 스스로 실패한 경우 자기 실패 처리를 마친 것으로 보고 참조 제거
                if (runningChild == child)
                {
                    runningChild = null;
                }

                continue;
            }

            if (childStatus == BTNodeStatus.Running)
            {
                ChangeRunningChild(child);
            }
            else
            {
                // 더 높은 우선순위의 자식이 즉시 성공했다면 이전에 실행 중이던 낮은 우선순위 행동을 중단
                CompleteSelectedChild(child);
            }

            return childStatus;
        }

        AbortRunningChild();

        // 모든 자식이 Failure를 반환했기 때문에 Selector도 실패 반환
        return BTNodeStatus.Failure;
    }

    private void ChangeRunningChild(BTNode nextChild)
    {
        if (runningChild != null && runningChild != nextChild)
        {
            runningChild.Abort();
        }

        runningChild = nextChild;
    }

    private void CompleteSelectedChild(BTNode child)
    {
        if (runningChild == child)
        {
            runningChild = null;
            return;
        }

        AbortRunningChild();
    }

    private void AbortRunningChild()
    {
        runningChild?.Abort();
        runningChild = null;
    }

    public override void Abort()
    {
        AbortRunningChild();
    }
}

// Composite Node : Sequence
public class BTSequence : BTNode
{
    private readonly BTNode[] children;

    private BTNode runningChild;

    public BTSequence(params BTNode[] children)
    {
        this.children = children;
    }

    public override BTNodeStatus Evaluate()
    {
        foreach (BTNode child in children)
        {
            BTNodeStatus childStatus = child.Evaluate();

            if (childStatus == BTNodeStatus.Success)
            {
                // Running 상태이던 Action이 성공 -> 정상 종료이므로 Abort 호출 X
                if (runningChild == child)
                {
                    runningChild = null;
                }

                continue;
            }

            // 앞 Condition이 실패한 경우 뒤에서 실행 중이던 이동/사격 Action을 즉시 중지
            if (childStatus == BTNodeStatus.Running)
            {
                ChangeRunningChild(child);

                return BTNodeStatus.Running;
            }

            if (runningChild == child)
            {
                runningChild = null;
            }
            else
            {
                AbortRunningChild();
            }

            return BTNodeStatus.Failure;
        }

        // 모든 자식이 Success 반환했기 때문에 Sequence도 성공 반환
        return BTNodeStatus.Success;
    }

    private void ChangeRunningChild(BTNode nextChild)
    {
        if (runningChild != null && runningChild != nextChild)
        {
            runningChild.Abort();
        }

        runningChild = nextChild;
    }

    private void AbortRunningChild()
    {
        runningChild?.Abort();
        runningChild = null;
    }

    public override void Abort()
    {
        AbortRunningChild();
    }
}

// Leaf Node : Action
public class BTAction : BTNode
{
    private readonly Func<BTNodeStatus> action;
    private readonly Action abortAction;

    public BTAction(Func<BTNodeStatus> action, Action abortAction = null)
    {
        this.action = action;
        this.abortAction = abortAction;
    }

    public override BTNodeStatus Evaluate()
    {
        return action();
    }

    public override void Abort()
    {
        // 행동이 강제로 중단된 경우에 실행
        abortAction?.Invoke();
    }
}

// Leaf Node : Condition
public class BTCondition : BTNode
{
    private readonly Func<bool> condition;

    public BTCondition(Func<bool> condition)
    {
        this.condition = condition;
    }

    public override BTNodeStatus Evaluate()
    {
        return condition() ? BTNodeStatus.Success : BTNodeStatus.Failure;
    }
}
