using UnityEngine;

namespace RTS
{
    public class Harvester : Unit
    {
        enum HState { Idle, ToField, Harvesting, ToRefinery, Unloading }
        HState state = HState.Idle;
        float load;
        const float Capacity = 700f;
        const float HarvestRate = 90f;
        TiberiumField field;
        Building refinery;
        float unloadDone;
        float retryAt;

        protected override void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.GameOver) return;
            if (data == null || Time.time < retryAt) return;

            switch (state)
            {
                case HState.Idle:
                    field = TiberiumField.FindNearest(transform.position);
                    if (field != null)
                    {
                        agent.stoppingDistance = 2.5f;
                        if (agent.isOnNavMesh) agent.SetDestination(field.transform.position);
                        state = HState.ToField;
                    }
                    else retryAt = Time.time + 1.5f;
                    break;

                case HState.ToField:
                    if (field == null) { state = HState.Idle; break; }
                    if (Arrived()) state = HState.Harvesting;
                    break;

                case HState.Harvesting:
                    if (field == null || field.amount <= 1f)
                    {
                        field = null;
                        if (load > 50f) GoRefinery(); else state = HState.Idle;
                        break;
                    }
                    load += field.Harvest(HarvestRate * Time.deltaTime);
                    if (load >= Capacity) { load = Capacity; GoRefinery(); }
                    break;

                case HState.ToRefinery:
                    if (refinery == null) { GoRefinery(); break; }
                    if (Arrived()) { state = HState.Unloading; unloadDone = Time.time + 1.5f; }
                    break;

                case HState.Unloading:
                    if (Time.time >= unloadDone)
                    {
                        ResourceManager.Instance.Add(team, Mathf.RoundToInt(load));
                        load = 0f;
                        state = HState.Idle;
                    }
                    break;
            }
        }

        void GoRefinery()
        {
            refinery = FindRefinery();
            if (refinery != null)
            {
                agent.stoppingDistance = 4.5f;
                if (agent.isOnNavMesh) agent.SetDestination(refinery.transform.position);
                state = HState.ToRefinery;
            }
            else
            {
                state = HState.ToRefinery;
                retryAt = Time.time + 2f;
            }
        }

        bool Arrived() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;

        Building FindRefinery()
        {
            Building best = null;
            float bd = float.MaxValue;
            foreach (var d in All)
            {
                if (d == null || d.team != team) continue;
                if (d is Building b && b.Constructed && b.data != null && b.data.id == "Refinery")
                {
                    float dist = (b.transform.position - transform.position).sqrMagnitude;
                    if (dist < bd) { bd = dist; best = b; }
                }
            }
            return best;
        }
    }
}
