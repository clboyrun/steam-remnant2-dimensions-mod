#include "PersistenceComponent.h"

UPersistenceComponent::UPersistenceComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->UniqueId = 0;
    this->HasModifiedSaveValues = false;
    this->PersistTransform = false;
    this->PersistDestroyed = false;
}


