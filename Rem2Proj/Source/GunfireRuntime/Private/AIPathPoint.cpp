#include "AIPathPoint.h"

AAIPathPoint::AAIPathPoint(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->Next = NULL;
    this->WaitTimeMin = 0.00f;
    this->WaitTimeMax = 0.00f;
    this->FacePathPointDirection = false;
}

AAIPathPoint* AAIPathPoint::GetNext(AAIPathPoint* Prev) {
    return NULL;
}


