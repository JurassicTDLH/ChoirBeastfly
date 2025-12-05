using ChoirBeastFly;
using UnityEngine;

namespace ChoirBeastfly.Behaviours;

/// <summary>
/// Modify the behavior of the Beastfly's corpse.
/// </summary>
internal class BeastflyCorpse : MonoBehaviour
{

    private void Awake()
    {
        ModifyPositionConstraints();
        DisableDrops();
    }

    /// <summary>
    /// Adjust position bounds in all <see cref="ConstrainPosition"/> components.
    /// </summary>
    private void ModifyPositionConstraints()
    {
        foreach (var constrainPos in GetComponentsInChildren<ConstrainPosition>(true))
        {
            constrainPos.xMin = ArenaBounds.XMin;
            constrainPos.xMax = ArenaBounds.XMax;
            constrainPos.yMin = ArenaBounds.YMin;
        }
    }
    private void DisableDrops()
    {
        GameObject itemchunk = transform.Find("Item Chunk").gameObject;
        GameObject collectableItemPickUp = itemchunk.transform.Find("Collectable Item Pickup").gameObject;
        collectableItemPickUp.SetActive(false);
    }
}
