#include "EnvQueryTest_Outside.h"

UEnvQueryTest_Outside::UEnvQueryTest_Outside() {
    this->TestPurpose = EEnvTestPurpose::Filter;
    this->FilterType = EEnvTestFilterType::Match;
    this->ScoringEquation = EEnvTestScoreEquation::Constant;
    this->TraceChannel = ECC_Visibility;
    this->NumRays = 12;
    this->Angle = 30.00f;
    this->InsideThresh = 9;
}


