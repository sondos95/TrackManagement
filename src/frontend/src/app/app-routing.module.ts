import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { TrackDetailPageComponent } from './components/track-detail/track-detail-page.component';
import { TrackListPageComponent } from './components/track-list/track-list-page.component';
import { sessionGuard } from './guards/session.guard';

const routes: Routes = [
  { path: '', pathMatch: 'full', component: HomeComponent, title: 'Takwene | Track Management' },
  { path: 'tracks', component: TrackListPageComponent, canActivate: [sessionGuard], title: 'Tracks | Track Management' },
  { path: 'tracks/:id', component: TrackDetailPageComponent, canActivate: [sessionGuard], title: 'Track detail | Track Management' },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
