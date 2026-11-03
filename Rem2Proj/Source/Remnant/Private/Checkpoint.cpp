#include "Checkpoint.h"

ACheckpoint::ACheckpoint(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bReplicates = true;
    const FProperty* p_RemoteRole = GetClass()->FindPropertyByName("RemoteRole");
    (*p_RemoteRole->ContainerPtrToValuePtr<TEnumAsByte<ENetRole>>(this)) = ROLE_SimulatedProxy;
}











bool ACheckpoint::CanCheckpointActivate_Implementation() const {
    return false;
}



