alter table ActualItemLocation add check ((OnHand > 0 and TotalCost >= 0) or (OnHand = 0 and TotalCost = 0))
