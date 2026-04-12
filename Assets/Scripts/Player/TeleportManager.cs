using UnityEngine;
using System.Collections.Generic;

public class TeleportManager : MonoBehaviour
{
   public List<TeleportPoint> teleportPoints = new List<TeleportPoint>();
   private int teleportPointIndex = 0;
   [SerializeField] private Player _player;

   public void NextTeleportPoint()
   {
      if (!(teleportPointIndex + 1 < teleportPoints.Count))
         return;
      if (!teleportPoints[teleportPointIndex+1].isChecked)
         return;
      teleportPointIndex++;
      if (teleportPointIndex >= teleportPoints.Count)
      {
         teleportPointIndex = 0;
      }
      Teleport();
   }

   public void BackTeleportPoint()
   {
      if (teleportPointIndex - 1 < 0)
         return;
      if (!teleportPoints[teleportPointIndex-1].isChecked)
         return;
      teleportPointIndex--;
      if (teleportPointIndex < 0)
      {
         teleportPointIndex = teleportPoints.Count - 1;
      }
      Teleport();
   }
   
   private void Teleport()
   {
      _player.transform.position = teleportPoints[teleportPointIndex].transform.position;
      
   }
}
